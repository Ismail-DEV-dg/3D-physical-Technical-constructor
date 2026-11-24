using CircuitEditor.Infrastructure.Repositories;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.App.Controls;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
using System.Windows.Media.Imaging;
using CircuitEditor.Core.Layout;

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
            var face = new Typeface(font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            Assert.True(face.TryGetGlyphTypeface(out var glyphs), font.Source);
            Assert.Contains("JetBrains Mono", glyphs.FamilyNames.Values);
            Assert.Contains('А', glyphs.CharacterToGlyphMap.Keys);
            VerifyAllSymbolsRender(resources);
            MockupPresentationChecks.Run(resources);
            BatchAPresentationChecks.Run(resources);
            BatchBPresentationChecks.Run(resources);
            BatchCPresentationChecks.Run(resources);
            RenderingCacheChecks.Run(resources);
            ResistorPresentationChecks.Run(resources);
            Stage4PresentationChecks.Run(resources);
            Stage5PresentationChecks.Run(resources);
            SmoothScrollPresentationChecks.Run(resources);
            UnoAndHoverPresentationChecks.Run(resources);
            PanelPresentationChecks.Run(resources);
            CodeEditorPresentationChecks.Run(resources);
            CodeWorkspacePresentationChecks.Run(resources);
            DiagnosticsPresentationChecks.Run(resources);
            SimulationPresentationChecks.Run(resources);
            ChipPresentationChecks.Run(resources);
            DialogLayoutChecks.Run(resources);
            application.Shutdown();
        });
    }

    private static void VerifyAllSymbolsRender(ResourceDictionary resources)
    {
        var calculator = new ComponentLayoutCalculator(new WpfTextMeasurer());
        var scene = new SchematicViewModel(TestProjectFactory.Create(new WpfTextMeasurer()));
        foreach (var definition in scene.Library)
        {
            var item = scene.AddComponent(definition, 100, 100);
            LayoutAssertions.Validate(item.Model, item.Layout);
            VerifyInkBounds(item.Layout);
            Assert.True(item.Model.TryUpdateProperties(new string('Ж', 80), item.Model.Color, item.Model.I2cAddress, out _));
            item.UpdateLayout();
            LayoutAssertions.Validate(item.Model, item.Layout);
            VerifyInkBounds(item.Layout);
        }
        Assert.Equal(27, scene.ComponentCount);
        var surface = new SchematicSurface { Scene = scene, Resources = resources,
            Background = (Brush)resources["BackgroundBrush"] };
        surface.Measure(new Size(800, 600));
        surface.Arrange(new Rect(0, 0, 800, 600));
        surface.UpdateLayout();
        var bitmap = new RenderTargetBitmap(800, 600, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        Assert.Equal(800, bitmap.PixelWidth);
        var font = new Typeface(WpfTextMeasurer.LabelFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        Assert.True(font.TryGetGlyphTypeface(out var glyphs));
        Assert.Contains("CircuitEditor.App;component/Resources/Fonts/", glyphs.FontUri.ToString());
        Assert.Equal(4, "081l".Select(character => glyphs.CharacterToGlyphMap[character]).Distinct().Count());
        var esp = scene.Components.Single(item => item.Model.Definition.Id == "esp32-devkit");
        // Use an isolated scene so no other debug component can cover the pin.
        var tooltipScene = new SchematicViewModel(TestProjectFactory.Create(new WpfTextMeasurer()));
        var tooltipItem = tooltipScene.AddComponent(esp.Model.Definition, 0, 0);
        var tooltipPin = tooltipItem.Layout.Pins.Single(item => item.Pin.Id == "GPIO34");
        surface.Scene = tooltipScene;
        var tip = surface.GetPinToolTip(new Point(tooltipPin.Position.X + tooltipScene.OffsetX,
            tooltipPin.Position.Y + tooltipScene.OffsetY));
        Assert.Contains("input only", tip);
        Assert.DoesNotContain("‹", tip);
        for (var turn = 0; turn < 4; turn++)
        {
            foreach (var zoom in new[] { 0.25, 1, 3 })
            {
                tooltipScene.Zoom = zoom;
                var world = LayoutTransform.ToWorld(tooltipItem.SchematicPlacement, tooltipItem.Layout, tooltipPin.Position);
                var rotatedTip = surface.GetPinToolTip(new Point(world.X * zoom + tooltipScene.OffsetX,
                    world.Y * zoom + tooltipScene.OffsetY));
                Assert.Contains("GPIO34\n", rotatedTip);
            }
            tooltipItem.SchematicPlacement.Rotate();
        }
    }

    private static void VerifyInkBounds(ComponentLayout layout)
    {
        foreach (var label in layout.AllLabels)
        {
            var formatted = WpfTextMeasurer.Format(label.Text, label.FontSize, Brushes.Black);
            var ink = formatted.BuildGeometry(new Point(label.Bounds.X + WpfTextMeasurer.InkPadding,
                label.Bounds.Y + WpfTextMeasurer.InkPadding)).Bounds;
            Assert.True(label.Bounds.Contains(new LayoutRect(ink.X, ink.Y, ink.Width, ink.Height)), label.Text);
        }
    }

    private static ResourceDictionary Load(string name) => new()
    {
        Source = new Uri($"/CircuitEditor.App;component/Resources/{name}.xaml", UriKind.Relative)
    };
}
