using System.Collections.ObjectModel;
using System.ComponentModel;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ProjectViewModel : ObservableObject, IDisposable
{
    private readonly IComponentLayoutCalculator layouts;
    public ProjectViewModel(IComponentLibraryRepository libraryRepository, IBreadboardRepository boardRepository,
        IFootprintRepository footprintRepository, IComponentLayoutCalculator layouts)
    {
        this.layouts = layouts;
        var library = libraryRepository.Load(); var board = boardRepository.Load(); var footprints = footprintRepository.Load();
        Library = library.Components;
        Model = new CircuitProject(footprints: footprints.Value,
            breadboards: board.Value is null ? [] : [new BreadboardInstance(Guid.NewGuid(), board.Value, 90, 5)]);
        StatusMessage = library.Error ?? board.Error ?? footprints.Error ??
            "Макет: перетащите компонент. Зелёные отверстия свободны; красные заняты.";
    }
    public CircuitProject Model { get; }
    public IReadOnlyList<ComponentDefinition> Library { get; }
    public ObservableCollection<ComponentViewModel> Components { get; } = [];
    public ComponentViewModel? SelectedComponent => Components.LastOrDefault(item => item.IsSelected);
    public int SelectedCount => Components.Count(item => item.IsSelected);
    public int ComponentCount => Components.Count;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private string propertyError = "";
    [ObservableProperty] private int revision;

    public ComponentViewModel? Add(ComponentDefinition definition)
    {
        var footprint = Model.Footprints.FirstOrDefault(item => item.ComponentDefinitionId == definition.Id);
        if (footprint is null) { StatusMessage = "Описание корпуса недоступно. Проверьте библиотеку."; return null; }
        var trayY = Components.Count == 0 ? 15 : Components.Max(existing => existing.MockupPlacement.Y +
            ContactGeometry.Bounds(existing.Footprint, existing.MockupPlacement,
                Model.Breadboards.FirstOrDefault()?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm).Bottom)
            + 2 * PhysicalSettings.FreeGridMm;
        var model = Model.AddComponent(definition);
        var item = new ComponentViewModel(model, Model, footprint, layouts);
        foreach (var other in Components) other.IsSelected = false;
        Components.Add(item);
        item.PropertyChanged += OnComponentChanged;
        // Give the inactive view a useful independent initial position.
        item.SchematicPlacement.MoveTo((Components.Count - 1) % 4 * 320, (Components.Count - 1) / 4 * 480);
        item.MockupPlacement.MoveTo(5, trayY);
        item.IsSelected = true;
        StatusMessage = $"Добавлен {item.Reference} · {item.Name}";
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
        var selected = Components.Where(item => item.IsSelected).ToArray();
        foreach (var item in selected)
        {
            item.PropertyChanged -= OnComponentChanged;
            Model.RemoveComponent(item.Model.Id); Components.Remove(item);
        }
        StatusMessage = $"Удалено компонентов: {selected.Length}";
        Refresh();
    }

    [RelayCommand]
    private void ApplyProperties()
    {
        if (SelectedComponent is not { } selected) return;
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
        Revision++;
        OnPropertyChanged(nameof(SelectedComponent));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(ComponentCount));
    }
    private void OnComponentChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ComponentViewModel.IsSelected)) Refresh();
        if (args.PropertyName == nameof(ComponentViewModel.ResistanceValue))
        {
            PropertyError = "";
            StatusMessage = "Номинал и цветные полосы резистора обновлены.";
            Refresh();
        }
    }
    public void Dispose()
    {
        foreach (var item in Components) item.PropertyChanged -= OnComponentChanged;
    }
}
