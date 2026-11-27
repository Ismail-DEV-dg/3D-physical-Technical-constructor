using System.ComponentModel;
using CircuitEditor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService navigation;
    private readonly IThemeService themes;
    private readonly IWindowService window;
    private readonly ICalculatorService? calculator;
    private readonly IShortcutGuideService? shortcutGuide;
    private readonly IErrorHelpService? errorHelp;
    private readonly IDialogService? dialogs;

    [ObservableProperty]
    private string statusMessage = "Готово к работе";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPaletteSelection))]
    private PaletteItemViewModel? selectedPaletteItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Palette))]
    private string paletteFilter = "";

    private IReadOnlyList<PaletteItemViewModel> library = [];

    public MainViewModel(INavigationService navigation, IThemeService themes, IWindowService window, ProjectSessionViewModel? session = null, ExportViewModel? exports = null, ProjectAnalysisViewModel? analysis = null, ICalculatorService? calculator = null, IShortcutGuideService? shortcutGuide = null,
        IClipboardService? clipboard=null,IErrorDocumentationService? documentation=null,IErrorHelpService? errorHelp=null, SimulationViewModel? simulation = null, IDialogService? dialogs = null, IShortcutSettings? shortcutSettings = null)
    {
        Simulation = simulation;
        Shortcuts = shortcutSettings ?? new ShortcutSettings();
        this.dialogs = dialogs;
        if (Simulation is not null)
        {
            Simulation.ConfigurationRequested += OpenSimulationPanel;
            Simulation.SketchStarted += ObserveSketch;
        }
        this.errorHelp=errorHelp;
        this.shortcutGuide=shortcutGuide;
        this.calculator=calculator;
        Analysis=analysis;
        if (analysis is not null) analysis.InspectRequested += OnInspectRequested;
        Session = session; Exports = exports;
        this.navigation = navigation;
        this.themes = themes;
        this.window = window;
        Schematic = navigation.Workspaces.OfType<SchematicViewModel>().Single();
        Mockup = navigation.Workspaces.OfType<BreadboardViewModel>().Single();
        // Basic parts must remain visible before the palette is scrolled.
        RefreshPalette();
        StatusMessage = Schematic.StatusMessage;
        Schematic.PropertyChanged += OnSchematicChanged;
        Mockup.PropertyChanged += OnSchematicChanged;
        Project.PropertyChanged += OnProjectChanged;
        navigation.PropertyChanged += OnNavigationChanged;
        Code.ShowCodeRequested+=OnShowCode;Code.PropertyChanged+=OnCodeChanged;
        Diagnostics=new(analysis,Code,clipboard,documentation,errorHelp,Simulation,Project);
        if (Simulation is not null) Simulation.FrameChanged += OnSimulationFrame;
        themes.PropertyChanged += OnThemeChanged;
        Chip.UndoCommand.CanExecuteChanged += OnChipHistoryChanged;
        Chip.RedoCommand.CanExecuteChanged += OnChipHistoryChanged;
    }

    public SimulationViewModel? Simulation { get; }
    public IShortcutSettings Shortcuts { get; }
    public ProjectAnalysisViewModel? Analysis { get; }
    public PanelLayoutViewModel Panels { get; } = new();
    public DiagnosticsViewModel Diagnostics {get;}
    private void ObserveSketch(object? sender, EventArgs e)
    {
        navigation.Navigate(WorkspaceKind.Mockup);
    }

    private void OnSimulationFrame(object? sender, Core.Simulation.SimulationState state)
    {
        Code.Output.ReceiveSerial(state);
        if (!state.IsHalted || state.Diagnostics.FirstOrDefault(d => d.SketchLine.HasValue) is not { } error) return;
        Diagnostics.Source = "Все";
        Diagnostics.Severity = "Все";
        Diagnostics.Search = "";
        Code.RevealProblem(new(error.Message, error.SketchLine!.Value, error.SketchColumn ?? 1, "Error", error.Code));
    }
    private void OpenSimulationPanel(object? sender, EventArgs e) => OnCodeTools(sender, 3);
    private void OnInspectRequested(object? sender, EventArgs e) => OnCodeTools(sender,Analysis?.InspectorTabIndex==2?1:0);
    [RelayCommand] private void Documentation(string? section) => errorHelp?.ShowSection(section ?? "home");
    [RelayCommand] private void ShowProblems(){Diagnostics.Source="Все";Diagnostics.Severity="Все";Diagnostics.Search="";OnCodeTools(this,0);}
    public ProjectSessionViewModel? Session { get; }
    public ExportViewModel? Exports { get; }
    public IReadOnlyList<WorkspaceViewModel> Workspaces => navigation.Workspaces;

    public WorkspaceViewModel SelectedWorkspace
    {
        get => navigation.Current;
        set
        {
            if (value is not null)
                navigation.Navigate(value.Kind);
        }
    }

    public bool IsMockup => SelectedWorkspace.Kind == WorkspaceKind.Mockup;
    public bool IsCanvas => SelectedWorkspace.Kind is WorkspaceKind.Mockup or WorkspaceKind.Schematic;

    public ThemeKind SelectedTheme
    {
        get => themes.CurrentTheme;
        set
        {
            if (!themes.SetTheme(value))
                StatusMessage = themes.ErrorMessage ?? "Тема недоступна.";
        }
    }

    public IReadOnlyList<PaletteItemViewModel> Palette => string.IsNullOrWhiteSpace(PaletteFilter)
        ? library : library.Where(item => item.Name.Contains(PaletteFilter, StringComparison.OrdinalIgnoreCase)
            || item.Category.Contains(PaletteFilter, StringComparison.OrdinalIgnoreCase)
            || item.Definition.Id.Contains(PaletteFilter, StringComparison.OrdinalIgnoreCase)
            || item.Definition.Name.Contains(PaletteFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
    public SchematicViewModel Schematic { get; }
    public BreadboardViewModel Mockup { get; }
    public ProjectViewModel Project => Schematic.Project;
    public CodeViewModel Code => navigation.Workspaces.OfType<CodeViewModel>().Single();
    private void OnShowCode(object? sender,EventArgs e){navigation.Navigate(WorkspaceKind.Code);OnCodeTools(sender,0);}
    private void OnCodeTools(object? sender,int index){Panels.Analysis.ShowCommand.Execute(null);Panels.ToolTabIndex=index;}
    private void OnChipHistoryChanged(object? sender, EventArgs e) { UndoActiveCommand.NotifyCanExecuteChanged(); RedoActiveCommand.NotifyCanExecuteChanged(); }
    private void OnCodeChanged(object? sender,PropertyChangedEventArgs e){UndoActiveCommand.NotifyCanExecuteChanged();RedoActiveCommand.NotifyCanExecuteChanged();}
    private ChipViewModel Chip => navigation.Workspaces.OfType<ChipViewModel>().Single();
    private bool CanUndoActive => SelectedWorkspace.Kind switch { WorkspaceKind.Code => Code.CanUndo, WorkspaceKind.Chip => Chip.UndoCommand.CanExecute(null), _ => Project.CanUndo };
    private bool CanRedoActive => SelectedWorkspace.Kind switch { WorkspaceKind.Code => Code.CanRedo, WorkspaceKind.Chip => Chip.RedoCommand.CanExecute(null), _ => Project.CanRedo };
    [RelayCommand(CanExecute=nameof(CanUndoActive))]
    private void UndoActive(){if(SelectedWorkspace.Kind==WorkspaceKind.Code)Code.EditorActionCommand.Execute("Undo");else if (SelectedWorkspace.Kind==WorkspaceKind.Chip) Chip.UndoCommand.Execute(null);else Project.UndoCommand.Execute(null);}
    [RelayCommand(CanExecute=nameof(CanRedoActive))]
    private void RedoActive(){if(SelectedWorkspace.Kind==WorkspaceKind.Code)Code.EditorActionCommand.Execute("Redo");else if (SelectedWorkspace.Kind==WorkspaceKind.Chip) Chip.RedoCommand.Execute(null);else Project.RedoCommand.Execute(null);}
    [RelayCommand] private void CopyActive(){if(SelectedWorkspace.Kind==WorkspaceKind.Code)Code.EditorActionCommand.Execute("Copy");else Project.CopyCommand.Execute(null);}
    [RelayCommand] private void PasteActive(){if(SelectedWorkspace.Kind==WorkspaceKind.Code)Code.EditorActionCommand.Execute("Paste");else Project.PasteCommand.Execute(null);}
    public int LibraryCount => library.Count;

    public bool HasPaletteSelection => SelectedPaletteItem is not null;
    public double CursorX => SelectedWorkspace.Kind == WorkspaceKind.Mockup ? Mockup.CursorX : Schematic.CursorX;
    public double CursorY => SelectedWorkspace.Kind == WorkspaceKind.Mockup ? Mockup.CursorY : Schematic.CursorY;
    public double ZoomPercent => SelectedWorkspace.Kind == WorkspaceKind.Mockup ? Mockup.ZoomPercent : Schematic.ZoomPercent;
    public int ComponentCount => Schematic.ComponentCount;
    public string CoordinateUnit => SelectedWorkspace.Kind == WorkspaceKind.Mockup ? "мм" : "px";

    private void OnSchematicChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Schematic.StatusMessage)) StatusMessage = Schematic.StatusMessage;
        if (args.PropertyName is nameof(CursorX) or nameof(CursorY) or nameof(ZoomPercent) or nameof(ComponentCount))
            OnPropertyChanged(args.PropertyName);
    }

    private void RefreshPalette()
    {
        library = Project.PaletteLibrary.OrderByDescending(definition => definition.Category == "ПАССИВНЫЕ")
            .Select(definition => new PaletteItemViewModel(definition)).ToArray();
        if (SelectedPaletteItem is { } current)
            SelectedPaletteItem = library.FirstOrDefault(item => item.Definition.Id == current.Definition.Id);
        OnPropertyChanged(nameof(Palette));
        OnPropertyChanged(nameof(LibraryCount));
    }

    private void OnProjectChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Project.PaletteLibrary)) RefreshPalette();
        if (args.PropertyName == nameof(Project.StatusMessage)) StatusMessage = Project.StatusMessage;
        if (args.PropertyName == nameof(Project.ComponentCount)) OnPropertyChanged(nameof(ComponentCount));
        if(args.PropertyName is nameof(Project.CanUndo) or nameof(Project.CanRedo)){UndoActiveCommand.NotifyCanExecuteChanged();RedoActiveCommand.NotifyCanExecuteChanged();}
    }

    [RelayCommand]
    private void ClearPaletteSearch() => PaletteFilter = "";

    [RelayCommand]
    private void SetZoom(string? value)
    {
        if (!IsCanvas || Project.InGesture || !int.TryParse(value, out var percent)) return;
        WorkspaceViewport.SetZoom(this, percent / 100d);
    }

    [RelayCommand]
    private void PlaceComponent(PaletteItemViewModel? item)
    {
        if (item is null || !Project.CanEdit) return;
        navigation.Navigate(WorkspaceKind.Mockup);
        Mockup.ArmPlacement(item.Definition.Id);
    }

    [RelayCommand]
    private void Navigate(WorkspaceKind kind)
    {
        if (!navigation.Navigate(kind))
            StatusMessage = "Рабочее пространство не найдено.";
    }

    [RelayCommand]
    private void ToggleTheme() => SelectedTheme = SelectedTheme == ThemeKind.NeonDark
        ? ThemeKind.LightTech : ThemeKind.NeonDark;

    [RelayCommand]
    private void SetTheme(ThemeKind theme) => SelectedTheme = theme;

    [RelayCommand]
    private void Explain(string action) =>
        StatusMessage = $"{action}: функция появится на следующих этапах.";

    [RelayCommand]
    private void ShowAllComponents()
    {
        navigation.Navigate(WorkspaceKind.Schematic);
        Schematic.ShowAllComponentsCommand.Execute(null);
    }

    [RelayCommand]
    private void Calculator()=>calculator?.Show();
    [RelayCommand]
    private void FitView()
    {if(SelectedWorkspace.Kind is WorkspaceKind.Mockup or WorkspaceKind.Schematic)WorkspaceViewport.Fit(this);}
    [RelayCommand]
    private void ShortcutHelp()=>shortcutGuide?.Show();
    [RelayCommand]
    private void About()
    {
        if (errorHelp is not null) errorHelp.ShowSection("about");
        else dialogs?.ShowAbout();
    }

    [RelayCommand]
    private void Minimize() => window.Minimize();

    [RelayCommand]
    private void ToggleMaximize() => window.ToggleMaximize();

    [RelayCommand]
    private void Close() => window.Close();

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(INavigationService.Current))
        {
            if (SelectedWorkspace.Kind == WorkspaceKind.Code) Code.EnsureEntryPoints();
            OnPropertyChanged(nameof(SelectedWorkspace));
            OnPropertyChanged(nameof(IsMockup));
            OnPropertyChanged(nameof(IsCanvas));
            UndoActiveCommand.NotifyCanExecuteChanged();RedoActiveCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CursorX)); OnPropertyChanged(nameof(CursorY)); OnPropertyChanged(nameof(ZoomPercent));
            OnPropertyChanged(nameof(CoordinateUnit));
        }
    }

    private void OnThemeChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(IThemeService.CurrentTheme))
        {
            OnPropertyChanged(nameof(SelectedTheme));
            StatusMessage = SelectedTheme == ThemeKind.NeonDark
                ? "Тема: Neon Dark" : "Тема: Light Tech";
        }
    }

    public void Dispose()
    {
        Diagnostics.Dispose();
        if (Simulation is not null)
        {
            Simulation.ConfigurationRequested -= OpenSimulationPanel;
            Simulation.SketchStarted -= ObserveSketch;
            Simulation.FrameChanged -= OnSimulationFrame;
        }
        Code.ShowCodeRequested-=OnShowCode;Code.PropertyChanged-=OnCodeChanged;
        if (Analysis is not null) Analysis.InspectRequested -= OnInspectRequested;
        navigation.PropertyChanged -= OnNavigationChanged;
        themes.PropertyChanged -= OnThemeChanged;
        Chip.UndoCommand.CanExecuteChanged -= OnChipHistoryChanged;
        Chip.RedoCommand.CanExecuteChanged -= OnChipHistoryChanged;
        Schematic.PropertyChanged -= OnSchematicChanged;
        Mockup.PropertyChanged -= OnSchematicChanged;
        Project.PropertyChanged -= OnProjectChanged;
    }
}
