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

    [ObservableProperty]
    private string statusMessage = "Готово к работе";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPaletteSelection))]
    private PaletteItemViewModel? selectedPaletteItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Palette))]
    private string paletteFilter = "";

    private readonly IReadOnlyList<PaletteItemViewModel> library;

    public MainViewModel(INavigationService navigation, IThemeService themes, IWindowService window, ProjectSessionViewModel? session = null, ExportViewModel? exports = null, ProjectAnalysisViewModel? analysis = null, ICalculatorService? calculator = null, IShortcutGuideService? shortcutGuide = null)
    {
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
        library = Schematic.Library.OrderByDescending(definition => definition.Category == "ПАССИВНЫЕ")
            .Select(definition => new PaletteItemViewModel(definition)).ToArray();
        StatusMessage = Schematic.StatusMessage;
        Schematic.PropertyChanged += OnSchematicChanged;
        Mockup.PropertyChanged += OnSchematicChanged;
        Project.PropertyChanged += OnProjectChanged;
        navigation.PropertyChanged += OnNavigationChanged;
        themes.PropertyChanged += OnThemeChanged;
    }

    public ProjectAnalysisViewModel? Analysis { get; }
    public PanelLayoutViewModel Panels { get; } = new();
    private void OnInspectRequested(object? sender, EventArgs e) => Panels.Inspector.ShowCommand.Execute(null);
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
            || item.Category.Contains(PaletteFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
    public SchematicViewModel Schematic { get; }
    public BreadboardViewModel Mockup { get; }
    public ProjectViewModel Project => Schematic.Project;
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

    private void OnProjectChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Project.StatusMessage)) StatusMessage = Project.StatusMessage;
        if (args.PropertyName == nameof(Project.ComponentCount)) OnPropertyChanged(nameof(ComponentCount));
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
    private void About() => StatusMessage = "Circuit Editor • Arduino / ESP32 • Курсовой проект • Этап 5";

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
            OnPropertyChanged(nameof(SelectedWorkspace));
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
        if (Analysis is not null) Analysis.InspectRequested -= OnInspectRequested;
        navigation.PropertyChanged -= OnNavigationChanged;
        themes.PropertyChanged -= OnThemeChanged;
        Schematic.PropertyChanged -= OnSchematicChanged;
        Mockup.PropertyChanged -= OnSchematicChanged;
        Project.PropertyChanged -= OnProjectChanged;
    }
}
