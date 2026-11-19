using CircuitEditor.Core.History;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ProjectViewModel : ObservableObject, IDisposable
{
    private readonly IComponentLayoutCalculator layouts;
    private readonly IUserChipLibraryService? chipLibrary;
    private readonly Core.Electronics.ElectricalCatalog? electricalCatalog;
    public ProjectViewModel(IComponentLibraryRepository libraryRepository, IBreadboardRepository boardRepository,
        IFootprintRepository footprintRepository, IComponentLayoutCalculator layouts, Core.Netlist.INetlistBuilder? netlistBuilder = null, IElectricalLibraryRepository? electricalRepository = null,
        IUserChipLibraryService? chipLibrary = null)
    {
        this.layouts = layouts; electricalCatalog=electricalRepository?.Load().Value;
        Wiring = new WireEditorViewModel(this);
        Wiring.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(WireEditorViewModel.Selected)) OnPropertyChanged(nameof(Wiring)); };
        var library = libraryRepository.Load(); var board = boardRepository.Load(); var footprints = footprintRepository.Load();
        Library = library.Components;
        this.chipLibrary = chipLibrary;
        if (chipLibrary is not null) chipLibrary.Changed += OnChipLibraryChanged;
        Model = new CircuitProject(footprints: footprints.Value,
            breadboards: board.Value is null ? [] : [new BreadboardInstance(Guid.NewGuid(), board.Value, PhysicalSettings.DefaultBoardX, PhysicalSettings.DefaultBoardY)]);
        Connectivity = new ProjectConnectivityViewModel(this,netlistBuilder);
        baseline = new CircuitEditor.Core.History.ProjectSnapshot(Model);
        History.Changed += (_, _) => NotifyHistory();
        StatusMessage = library.Error ?? board.Error ?? footprints.Error ??
            "Макет: перетащите компонент. Зелёные отверстия свободны; красные заняты.";
    }
    public ProjectAnalysisViewModel? Analysis { get; set; }
    public void InvalidatePresentation()=>Revision++;
    public ProjectConnectivityViewModel Connectivity { get; }
    public event EventHandler? ModelCommitted;
    public WireEditorViewModel Wiring { get; }
    public CircuitProject Model { get; }
    public IReadOnlyList<ComponentDefinition> Library { get; }
    public IReadOnlyList<ComponentDefinition> PaletteLibrary => chipLibrary is null ? Library : Library.Concat(chipLibrary.Palette).ToArray();
    private void OnChipLibraryChanged(object? sender, EventArgs args) => OnPropertyChanged(nameof(PaletteLibrary));
    public ObservableCollection<ComponentViewModel> Components { get; } = [];
    public ComponentViewModel? SelectedComponent => Components.LastOrDefault(item => item.IsSelected);
    public int SelectedCount => Components.Count(item => item.IsSelected);
    public int ComponentCount => Components.Count;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private string propertyError = "";
    [ObservableProperty] private int revision;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanEdit))] private bool isSimulationActive;
    public bool CanEdit => !IsSimulationActive;
    partial void OnIsSimulationActiveChanged(bool value) => NotifyHistory();

    public ComponentViewModel? Add(ComponentDefinition definition)
    {
        if (!CanEdit) return null;
        var footprint = Model.Footprints.FirstOrDefault(item => item.ComponentDefinitionId == definition.Id);
        var chip = footprint is null ? chipLibrary?.Definitions.FirstOrDefault(item => item.ComponentId == definition.Id) : null;
        if (footprint is null && chip is null) { StatusMessage = "Описание корпуса недоступно. Проверьте библиотеку."; return null; }
        var trayY = Components.Count == 0 ? 15 : Components.Max(existing => existing.MockupPlacement.Y +
            ContactGeometry.Bounds(existing.Footprint, existing.MockupPlacement,
                Model.Breadboards.FirstOrDefault()?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm).Bottom)
            + 2 * PhysicalSettings.FreeGridMm;
        var own = BeginGesture("Добавление компонента");
        if (chip is not null)
        {
            Model.RegisterChip(chip);
            footprint = Model.Footprints.Single(item => item.ComponentDefinitionId == definition.Id);
        }
        var model = Model.AddComponent(definition);
        var item = new ComponentViewModel(model, Model, footprint!, layouts, electricalCatalog);
        foreach (var other in Components) other.IsSelected = false;
        Components.Add(item);
        item.PropertyChanged += OnComponentChanged;
        // Give the inactive view a useful independent initial position.
        item.SchematicPlacement.MoveTo((Components.Count - 1) % 4 * 320, (Components.Count - 1) / 4 * 480);
        item.MockupPlacement.MoveTo(5, trayY);
        item.IsSelected = true;
        StatusMessage = $"Добавлен {item.Reference} · {item.Name}";
        if (own) EndGesture();
        Refresh();
        return item;
    }

    public void Select(ComponentViewModel? item, bool additive = false)
    {
        if (!additive) foreach (var other in Components) if (other != item) other.IsSelected = false;
        if (item is not null) item.IsSelected = additive ? !item.IsSelected : true;
        Refresh();
    }

    public void RemoveSelected()
    {
        if (!CanEdit) return;
        var own = BeginGesture("Удаление компонентов");
        var selected = Components.Where(item => item.IsSelected).ToArray();
        foreach (var item in selected)
        {
            item.PropertyChanged -= OnComponentChanged;
            Model.RemoveComponent(item.Model.Id); Components.Remove(item);
        }
        if (own) EndGesture();
        StatusMessage = $"Удалено компонентов: {selected.Length}";
        Refresh();
    }

    [RelayCommand]
    private void ApplyProperties()
    {
        if (!CanEdit || SelectedComponent is not { } selected) return;
        if (!selected.Model.TryUpdateProperties(selected.EditValue, selected.EditColor,
            selected.SupportsI2c ? selected.EditI2cAddress : null, out var error))
        { PropertyError = error ?? "Некорректное значение."; return; }
        PropertyError = "";
        selected.UpdateLayout();
        StatusMessage = $"Свойства {selected.Reference} обновлены в обоих представлениях.";
        Refresh();
    }

    public void Refresh()
    {
        Wiring.Refresh();
        RecordChanges();
        Revision++;
        OnPropertyChanged(nameof(SelectedComponent));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(ComponentCount));
    }
    private void OnComponentChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ComponentViewModel.MutationVersion)) { RecordChanges(); return; }
        if (args.PropertyName == nameof(ComponentViewModel.IsSelected)) Refresh();
        if (args.PropertyName is nameof(ComponentViewModel.ResistanceValue) or nameof(ComponentViewModel.AppearanceVersion))
        {
            PropertyError = "";
            StatusMessage = "Свойства компонента и макет обновлены.";
            Refresh();
        }
    }
    public void Dispose()
    {
        if (chipLibrary is not null) chipLibrary.Changed -= OnChipLibraryChanged;
        foreach (var item in Components) item.PropertyChanged -= OnComponentChanged;
    }
    public CommandHistory History { get; } = new();
    private Core.Code.SketchDocument savedSketch = new();
    private SimulationSetup savedSimulationSetup = new();
    public event EventHandler? SketchChanged;
    public event EventHandler? ProjectReplaced;
    public void UpdateSketch(Core.Code.SketchDocument sketch)
    {
        if (Model.Sketch == sketch) return;
        Model.SetSketch(sketch);OnPropertyChanged(nameof(IsDirty));SketchChanged?.Invoke(this,EventArgs.Empty);
    }
    public void UpdateSimulationSetup(SimulationSetup setup)
    {
        if (Model.SimulationSetup.Matches(setup)) return;
        Model.SetSimulationSetup(setup);
        OnPropertyChanged(nameof(IsDirty));
    }

    public void MarkSaved()
    {
        savedSketch=Model.Sketch;savedSimulationSetup=Model.SimulationSetup;History.MarkSaved();OnPropertyChanged(nameof(IsDirty));
    }
    private ProjectSnapshot baseline = null!;
    private bool inGesture, restoring;
    private string gestureName = "Изменение проекта";
    private SelectionClipboard? clipboard;
    public bool IsDirty => History.IsDirty || Model.Sketch != savedSketch || !Model.SimulationSetup.Matches(savedSimulationSetup);
    public bool CanUndo => CanEdit && History.CanUndo && !inGesture;
    public bool CanRedo => CanEdit && History.CanRedo && !inGesture;
    public bool InGesture => inGesture;
    public bool BeginGesture(string name = "Изменение проекта")
    {
        if (!CanEdit || inGesture) return false;
        inGesture = true; gestureName = name; NotifyHistory(); return true;
    }
    public void EndGesture()
    {
        if (!inGesture) return;
        inGesture = false; RecordChanges(gestureName); NotifyHistory();
    }
    public void CancelGesture()
    {
        if (!inGesture) return;
        inGesture = false;
        if (!baseline.Matches(Model)) RestoreState(baseline.Restore());
        NotifyHistory();
    }
    private void RecordChanges(string name = "Свойства / размещение")
    {
        if (restoring || inGesture || baseline is null || baseline.Matches(Model)) return;
        var after = new ProjectSnapshot(Model); var before = baseline; baseline = after;
        History.Record(new ProjectEditCommand(name, before, after, RestoreState));
        Committed();
    }
    public void ReplaceProject(CircuitProject state)
    {
        Model.SetSketch(state.Sketch);savedSketch=state.Sketch;
        Model.SetSimulationSetup(state.SimulationSetup);savedSimulationSetup=state.SimulationSetup;
        inGesture = false; RestoreState(state); History.Clear(); PropertyError = "";
        ProjectReplaced?.Invoke(this,EventArgs.Empty);SketchChanged?.Invoke(this,EventArgs.Empty);NotifyHistory();
    }
    private void RestoreState(CircuitProject state)
    {
        var selected = Components.Where(c => c.IsSelected).Select(c => c.Model.Id).ToHashSet();
        restoring = true;
        try
        {
            foreach (var c in Components) c.PropertyChanged -= OnComponentChanged;
            Components.Clear(); Model.Restore(state);
            foreach (var c in Model.Circuit.Components)
            {
                var footprint = Model.Footprints.Single(f => f.ComponentDefinitionId == c.Definition.Id);
                var vm = new ComponentViewModel(c, Model, footprint, layouts, electricalCatalog) { IsSelected = selected.Contains(c.Id) };
                vm.PropertyChanged += OnComponentChanged; Components.Add(vm);
            }
            baseline = new ProjectSnapshot(Model); Refresh();
        }
        finally { restoring = false; }
        Committed();
    }
    private void Committed()
    {
        Model.SetSimulation(new());
        Connectivity.Rebuild();
        ModelCommitted?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(Connectivity));
    }
    private void NotifyHistory()
    {
        OnPropertyChanged(nameof(IsDirty)); OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged(); RedoCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() { Wiring.Cancel(); History.Undo(); StatusMessage = "Действие отменено."; }
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() { Wiring.Cancel(); History.Redo(); StatusMessage = "Действие повторено."; }
    [RelayCommand]
    private void Copy()
    {
        clipboard = new SelectionClipboard(Model, Components.Where(c => c.IsSelected).Select(c => c.Model.Id));
        StatusMessage = "Выбранные компоненты и внутренние перемычки скопированы.";
    }
    [RelayCommand]
    private void Paste()
    {
        if (!CanEdit || clipboard is null || inGesture) return;
        BeginGesture("Вставка копии");
        var ids = clipboard.Paste(Model).ToHashSet();
        // Rebuild bindings without replacing the pre-gesture snapshot.
        var before = baseline; RestoreState(new ProjectSnapshot(Model).Restore()); baseline = before;
        foreach (var c in Components) c.IsSelected = ids.Contains(c.Model.Id);
        EndGesture(); Refresh();
    }
}
