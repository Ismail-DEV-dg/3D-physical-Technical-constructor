using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CircuitEditor.App.Services;

namespace CircuitEditor.Tests;

public sealed class ThemeResourceTests
{
    [Fact]
    public void RealDictionariesHaveMatchingKeysAndSwitchWithoutReplacingStyles()
    {
        StaRunner.Run(() =>
        {
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var resources = new ResourceDictionary();
            resources.MergedDictionaries.Add(Load("Themes/NeonDark"));
            var styles = Load("Styles");
            resources.MergedDictionaries.Add(styles);
            var darkKeys = resources.MergedDictionaries[0].Keys.Cast<string>().Order().ToArray();
            var service = new ThemeService(new WpfThemeResources(resources));

            Assert.True(service.SetTheme(ThemeKind.LightTech), service.ErrorMessage);
            Assert.Equal(Color.FromRgb(0xED, 0xF2, 0xF8), resources["BackgroundColor"]);
            Assert.Equal(darkKeys, resources.MergedDictionaries[0].Keys.Cast<string>().Order().ToArray());
            Assert.Same(styles, resources.MergedDictionaries[1]);

            Assert.True(service.SetTheme(ThemeKind.NeonDark), service.ErrorMessage);
            Assert.Equal(Color.FromRgb(0x0A, 0x0E, 0x17), resources["BackgroundColor"]);
            foreach (var type in new[] { typeof(Button), typeof(TextBox), typeof(ComboBox),
                         typeof(TabControl), typeof(System.Windows.Controls.Primitives.ScrollBar),
                         typeof(ToolTip), typeof(Menu), typeof(ListBox) })
            {
                var style = Assert.IsType<Style>(resources[type]);
                Assert.Contains(style.Setters.Cast<Setter>(), setter => setter.Property == Control.TemplateProperty);
            }
            var font = Assert.IsType<FontFamily>(resources["MonoFont"]);
            Assert.Contains(font.GetTypefaces(), face => face.TryGetGlyphTypeface(out var glyphs)
                && glyphs.CharacterToGlyphMap.ContainsKey('А'));
            application.Shutdown();
        });
    }

    private static ResourceDictionary Load(string name) => new()
    {
        Source = new Uri($"/CircuitEditor.App;component/Resources/{name}.xaml", UriKind.Relative)
    };
}

