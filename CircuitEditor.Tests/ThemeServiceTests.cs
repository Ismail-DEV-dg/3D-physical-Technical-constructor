using CircuitEditor.App.Services;

namespace CircuitEditor.Tests;

public sealed class ThemeServiceTests
{
    [Fact]
    public void DefaultThemeIsNeonDark()
    {
        var service = new ThemeService(new TestThemeResources());
        Assert.Equal(ThemeKind.NeonDark, service.CurrentTheme);
        Assert.Null(service.ErrorMessage);
    }

    [Fact]
    public void SwitchingBothDirectionsAppliesResourcesAndNotifies()
    {
        var resources = new TestThemeResources();
        var service = new ThemeService(resources);
        var changes = new List<string?>();
        service.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        Assert.True(service.SetTheme(ThemeKind.LightTech));
        Assert.Equal(ThemeKind.LightTech, service.CurrentTheme);
        Assert.True(service.SetTheme(ThemeKind.NeonDark));
        Assert.Equal(ThemeKind.NeonDark, service.CurrentTheme);
        Assert.Equal([ThemeKind.LightTech, ThemeKind.NeonDark], resources.Applied);
        Assert.Equal(2, changes.Count(name => name == nameof(service.CurrentTheme)));
    }

    [Fact]
    public void SettingCurrentThemeDoesNotReloadResources()
    {
        var resources = new TestThemeResources();
        var service = new ThemeService(resources);
        Assert.True(service.SetTheme(ThemeKind.NeonDark));
        Assert.Empty(resources.Applied);
    }

    [Fact]
    public void InvalidThemeDoesNotChangeStateOrResources()
    {
        var resources = new TestThemeResources();
        var service = new ThemeService(resources);
        Assert.False(service.SetTheme((ThemeKind)int.MaxValue));
        Assert.Equal(ThemeKind.NeonDark, service.CurrentTheme);
        Assert.NotNull(service.ErrorMessage);
        Assert.Empty(resources.Applied);
    }

    [Fact]
    public void ResourceFailureKeepsCurrentThemeAndAllowsRetry()
    {
        var resources = new TestThemeResources { FailOnApply = true };
        var service = new ThemeService(resources);
        Assert.False(service.SetTheme(ThemeKind.LightTech));
        Assert.Equal(ThemeKind.NeonDark, service.CurrentTheme);
        Assert.NotNull(service.ErrorMessage);
        resources.FailOnApply = false;
        Assert.True(service.SetTheme(ThemeKind.LightTech));
        Assert.Null(service.ErrorMessage);
    }
}

