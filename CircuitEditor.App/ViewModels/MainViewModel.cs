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

    [ObservableProperty]
    private string statusMessage = "Готово к работе";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPaletteSelection))]
    private PaletteItemViewModel? selectedPaletteItem;

    public MainViewModel(INavigationService navigation, IThemeService themes, IWindowService window)
    {
        this.navigation = navigation;
        this.themes = themes;
        this.window = window;
        navigation.PropertyChanged += OnNavigationChanged;
        themes.PropertyChanged += OnThemeChanged;
    }

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

    public IReadOnlyList<PaletteItemViewModel> Palette { get; } =
        Array.AsReadOnly<PaletteItemViewModel>(
        [
            new("Arduino Uno", "КОНТРОЛЛЕР", "µ", "Плата Arduino Uno.\nКарточка библиотеки компонентов."),
            new("ESP32 DevKit", "КОНТРОЛЛЕР", "µ", "Плата ESP32 DevKit.\nКарточка библиотеки компонентов."),
            new("Резистор", "ПАССИВНЫЙ", "R", "Резистор.\nКарточка библиотеки компонентов."),
            new("Светодиод", "ОПТОЭЛЕКТРОНИКА", "D", "Светодиод.\nКарточка библиотеки компонентов."),
            new("Кнопка", "ПЕРЕКЛЮЧАТЕЛЬ", "S", "Тактовая кнопка.\nКарточка библиотеки компонентов.")
        ]);

    public bool HasPaletteSelection => SelectedPaletteItem is not null;
    // Until the schematic editor exists these are the initial, empty document values.
    public double CursorX => 0;
    public double CursorY => 0;
    public double ZoomPercent => 100;
    public int ComponentCount => 0;

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
    private void About() => StatusMessage = "Circuit Editor • Arduino / ESP32 • Курсовой проект • Этап 1";

    [RelayCommand]
    private void Minimize() => window.Minimize();

    [RelayCommand]
    private void ToggleMaximize() => window.ToggleMaximize();

    [RelayCommand]
    private void Close() => window.Close();

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(INavigationService.Current))
            OnPropertyChanged(nameof(SelectedWorkspace));
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
        navigation.PropertyChanged -= OnNavigationChanged;
        themes.PropertyChanged -= OnThemeChanged;
    }
}

