using System.Windows;

namespace CircuitEditor.App.Services;

public sealed class WpfThemeResources(ResourceDictionary applicationResources) : IThemeResources
{
    public void Apply(ThemeKind theme)
    {
        var file = theme switch
        {
            ThemeKind.NeonDark => "NeonDark",
            ThemeKind.LightTech => "LightTech",
            _ => throw new ArgumentOutOfRangeException(nameof(theme))
        };
        // Load completely before replacing the current dictionary.
        var replacement = new ResourceDictionary
        {
            Source = new Uri($"/CircuitEditor.App;component/Resources/Themes/{file}.xaml", UriKind.Relative)
        };
        applicationResources.MergedDictionaries[0] = replacement;
    }
}
