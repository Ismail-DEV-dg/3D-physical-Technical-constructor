using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.Tests;

public sealed class MainViewModelTests
{
    private static NavigationService CreateNavigation()
    {
        var project = TestProjectFactory.Create();
        return new NavigationService(TestProjectFactory.Mockup(project), new SchematicViewModel(project),
            new ChipViewModel(), new CodeViewModel());
    }

    [Theory]
    [InlineData(WorkspaceKind.Mockup, typeof(BreadboardViewModel))]
    [InlineData(WorkspaceKind.Schematic, typeof(SchematicViewModel))]
    [InlineData(WorkspaceKind.Chip, typeof(ChipViewModel))]
    [InlineData(WorkspaceKind.Code, typeof(CodeViewModel))]
    [InlineData(WorkspaceKind.ThreeD, typeof(ThreeDViewModel))]
    public void NavigationCommandSelectsViewModel(WorkspaceKind destination, Type expectedType)
    {
        var navigation = CreateNavigation();
        using var model = new MainViewModel(navigation,
            new ThemeService(new TestThemeResources()), new TestWindowService());

        model.NavigateCommand.Execute(destination);

        Assert.IsType(expectedType, model.SelectedWorkspace);
        Assert.Same(navigation.Current, model.SelectedWorkspace);
    }

    [Fact]
    public void TabSelectionAndExternalNavigationNotifyBindingsAndReuseInstances()
    {
        var navigation = CreateNavigation();
        using var model = new MainViewModel(navigation,
            new ThemeService(new TestThemeResources()), new TestWindowService());
        var initial = model.SelectedWorkspace;
        var changes = new List<string?>();
        model.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        model.SelectedWorkspace = model.Workspaces.Single(item => item.Kind == WorkspaceKind.Code);
        Assert.Equal(WorkspaceKind.Code, navigation.Current.Kind);
        navigation.Navigate(WorkspaceKind.Mockup);

        Assert.Same(initial, model.SelectedWorkspace);
        Assert.Equal(2, changes.Count(name => name == nameof(model.SelectedWorkspace)));
    }

    [Fact]
    public void InvalidNavigationKeepsSelectionAndReportsError()
    {
        using var model = new MainViewModel(CreateNavigation(),
            new ThemeService(new TestThemeResources()), new TestWindowService());
        var previous = model.SelectedWorkspace;
        model.NavigateCommand.Execute((WorkspaceKind)int.MaxValue);
        Assert.Same(previous, model.SelectedWorkspace);
        Assert.Equal("Рабочее пространство не найдено.", model.StatusMessage);
    }

    [Fact]
    public void ThemeCommandsAndExternalChangesUpdateViewModel()
    {
        var service = new ThemeService(new TestThemeResources());
        using var model = new MainViewModel(CreateNavigation(), service, new TestWindowService());
        var changes = new List<string?>();
        model.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        model.ToggleThemeCommand.Execute(null);
        Assert.Equal(ThemeKind.LightTech, model.SelectedTheme);
        Assert.Equal(ThemeKind.LightTech, service.CurrentTheme);
        model.SetThemeCommand.Execute(ThemeKind.NeonDark);
        Assert.Equal(ThemeKind.NeonDark, model.SelectedTheme);
        service.SetTheme(ThemeKind.LightTech);
        Assert.Equal(ThemeKind.LightTech, model.SelectedTheme);
        Assert.Equal(3, changes.Count(name => name == nameof(model.SelectedTheme)));
    }

    [Fact]
    public void ThemeFailureIsReportedWithoutChangingSelection()
    {
        var resources = new TestThemeResources { FailOnApply = true };
        using var model = new MainViewModel(CreateNavigation(),
            new ThemeService(resources), new TestWindowService());
        model.ToggleThemeCommand.Execute(null);
        Assert.Equal(ThemeKind.NeonDark, model.SelectedTheme);
        Assert.Contains("Не удалось загрузить тему", model.StatusMessage);
    }

    [Fact]
    public void ThemeChangePreservesWorkspaceAndPaletteSelection()
    {
        using var model = new MainViewModel(CreateNavigation(),
            new ThemeService(new TestThemeResources()), new TestWindowService());
        model.NavigateCommand.Execute(WorkspaceKind.Chip);
        var selected = model.SelectedWorkspace;
        model.SelectedPaletteItem = model.Palette[0];
        model.ToggleThemeCommand.Execute(null);
        Assert.Same(selected, model.SelectedWorkspace);
        Assert.Same(model.Palette[0], model.SelectedPaletteItem);
        Assert.True(model.HasPaletteSelection);
        Assert.Equal(0, model.ComponentCount);
    }

    [Fact]
    public void ResistorIsFirstAndRemainsAvailableAfterClearingSearch()
    {
        using var model = new MainViewModel(CreateNavigation(),
            new ThemeService(new TestThemeResources()), new TestWindowService());
        Assert.Equal("resistor", model.Palette[0].Definition.Id);
        Assert.Equal(28, model.Palette.Count);
        model.PaletteFilter = "Резистор";
        Assert.Contains(model.Palette, item => item.Definition.Id == "resistor");
        model.PaletteFilter = "Arduino";
        Assert.DoesNotContain(model.Palette, item => item.Definition.Id == "resistor");
        model.PaletteFilter = "";
        Assert.Equal("resistor", model.Palette[0].Definition.Id);
        Assert.Equal(28, model.Palette.Count);
    }

    [Fact]
    public void DisposeUnsubscribesFromServices()
    {
        var navigation = CreateNavigation();
        var themes = new ThemeService(new TestThemeResources());
        var model = new MainViewModel(navigation, themes, new TestWindowService());
        model.Dispose();
        var notifications = 0;
        model.PropertyChanged += (_, _) => notifications++;
        themes.SetTheme(ThemeKind.LightTech);
        navigation.Navigate(WorkspaceKind.Code);
        Assert.Equal(0, notifications);
    }
}
