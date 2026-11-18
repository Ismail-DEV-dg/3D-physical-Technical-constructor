using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Services;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Tests;

internal static class BatchCPresentationChecks
{
    private static readonly string[] Ids = ["dht11", "dht22", "hc-sr04", "pir", "photoresistor", "thermistor", "rgb-led", "switch", "battery"];
    public static void Run(ResourceDictionary resources)
    {
        var themes = new ThemeService(new WpfThemeResources(resources));
        foreach (var theme in new[] { ThemeKind.NeonDark, ThemeKind.LightTech })
        {
            Assert.True(themes.SetTheme(theme));
            using var project = TestProjectFactory.Create(new WpfTextMeasurer());
            using var scene = TestProjectFactory.Mockup(project);
            var cache = new ComponentDrawingCache(); var atlas = new DrawingGroup();
            using (var dc = atlas.Open())
            {
                dc.DrawRectangle((Brush)resources["BackgroundBrush"], null, new Rect(0, 0, 1100, 760));
                for (var index = 0; index < Ids.Length; index++)
                {
                    var item = project.Add(project.Library.Single(item => item.Id == Ids[index]))!;
                    var pose = item.MockupPlacement.Copy();
                    for (var rotation = 0; rotation < 360; rotation += 90)
                    {
                        while (pose.Rotation != rotation) pose.Rotate();
                        var bounds = ContactGeometry.Bounds(item.Footprint, pose, 2.54);
                        var drawing = new DrawingGroup();
                        using (var draw = drawing.Open())
                        {
                            draw.PushTransform(new RotateTransform(rotation));
                            draw.DrawDrawing(cache.Get(item, pose, 2.54));
                        }
                        var pixels = Pixels(drawing, bounds.X - 6, bounds.Y - 6);
                        Assert.True(pixels.Distinct().Take(16).Count() >= 16, item.Name);
                        item.MockupPlacement.MoveTo(50, 50);
                        while (item.MockupPlacement.Rotation != rotation) item.MockupPlacement.Rotate();
                        var surface = new BreadboardSurface { Scene = scene };
                        foreach (var contact in item.Footprint.Contacts)
                        {
                            var position = ContactGeometry.World(item.Footprint, contact, item.MockupPlacement, 2.54);
                            var tip = surface.GetPinToolTip(new Point(PhysicalSettings.ToPixels(position.X) + scene.OffsetX,
                                PhysicalSettings.ToPixels(position.Y) + scene.OffsetY));
                            Assert.Contains($"контакт {contact.Id}\n", tip);
                            if (contact.ElectricalPinId == "NC") Assert.Contains("внутреннего электрического соединения нет", tip);
                        }
                    }
                    var first = cache.Get(item, new(), 2.54);
                    foreach (var parameter in item.InputsEditor!.Parameters)
                    {
                        var before = cache.Get(item, new(), 2.54);
                        parameter.Value = parameter.Definition.Input == ComponentInput.BatteryVoltage ? 1.5 : parameter.Definition.Maximum;
                        var after = cache.Get(item, new(), 2.54);
                        if (ComponentInputDefinitions.ChangesAppearance(parameter.Definition.Input))
                        {
                            Assert.NotSame(before, after);
                            Assert.False(Pixels(before, item.Footprint.Body.X - 6, item.Footprint.Body.Y - 6)
                                .SequenceEqual(Pixels(after, item.Footprint.Body.X - 6, item.Footprint.Body.Y - 6)));
                        }
                        else Assert.Same(before, after);
                    }
                    var col = index % 3; var row = index / 3;
                    BoardDetailDrawing.TextCentered(dc, item.Name, col * 360 + 170, row * 250 + 12, 14, (Brush)resources["TextBrush"]);
                    var local = ContactGeometry.Bounds(item.Footprint, new(), 2.54);
                    dc.PushTransform(new TranslateTransform(col * 360 + 25, row * 250 + 40));
                    dc.PushTransform(new ScaleTransform(5, 5));
                    dc.PushTransform(new TranslateTransform(6 - local.X, 6 - local.Y));
                    dc.DrawDrawing(cache.Get(item, new(), 2.54));
                    dc.Pop(); dc.Pop(); dc.Pop();
                }
            }
            var output = Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_VISUAL_CHECK");
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(output);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(Bitmap(atlas, 0, 0, 1, 1100, 760)));
                using var file = File.Create(Path.Combine(output, $"batch-c-{theme}.png")); encoder.Save(file);
            }
            VerifyInputs(resources);
        }
        Assert.True(themes.SetTheme(ThemeKind.NeonDark));
    }
    private static void VerifyInputs(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        using var project = TestProjectFactory.Create(new WpfTextMeasurer());
        var dht = project.Add(project.Library.Single(item => item.Id == "dht11"))!;
        var view = new PropertiesView { DataContext = project };
        var window = new Window { Content = view, Width = 260, Height = 1000, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        window.Show();
        try
        {
            window.UpdateLayout();
            var sliders = Descendants(view).OfType<Slider>().Where(slider => slider.IsVisible).ToArray();
            Assert.Equal(2, sliders.Length);
            var temperature = sliders.Single(slider => AutomationProperties.GetName(slider) == "Температура");
            Assert.Equal(0, temperature.Minimum); Assert.Equal(50, temperature.Maximum);
            Assert.Contains(((Style)resources[typeof(Slider)]).Setters.Cast<Setter>(), setter => setter.Property == Control.TemplateProperty);
            var revision = project.Revision;
            temperature.SetCurrentValue(Slider.ValueProperty, 37d);
            Assert.Equal(37, dht.Model.Inputs.Values[ComponentInput.Temperature]);
            Assert.Equal(revision, project.Revision);
            var battery = project.Add(project.Library.Single(item => item.Id == "battery"))!; window.UpdateLayout();
            var choice = Descendants(view).OfType<ComboBox>().Single(combo => combo.IsVisible && AutomationProperties.GetName(combo) == "Напряжение");
            Assert.Equal(6, choice.Items.Count); choice.SelectedValue = 3.7;
            Assert.Equal(3.7, battery.Voltage); Assert.Equal("3.7 В", battery.Model.Value);
            Assert.DoesNotContain(Descendants(view).OfType<TextBox>(), box => box.IsVisible && AutomationProperties.GetName(box) == "Номинал");
            var pir = project.Add(project.Library.Single(item => item.Id == "pir"))!; window.UpdateLayout();
            choice = Descendants(view).OfType<ComboBox>().Single(combo => combo.IsVisible && AutomationProperties.GetName(combo) == "Движение");
            choice.SelectedValue = 1d; Assert.Equal(1, pir.Model.Inputs.Values[ComponentInput.Motion]);
            project.Select(dht); window.UpdateLayout();
            Assert.Equal(37, Descendants(view).OfType<Slider>().Single(slider => slider.IsVisible && AutomationProperties.GetName(slider) == "Температура").Value);
        }
        finally { window.Close(); }
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static int[] Pixels(Drawing drawing, double x, double y)
    {
        var bitmap = Bitmap(drawing, x, y, 6, 600, 600); var pixels = new int[600 * 600];
        bitmap.CopyPixels(pixels, 600 * sizeof(int), 0); return pixels;
    }
    private static RenderTargetBitmap Bitmap(Drawing drawing, double x, double y, double scale, int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale)); dc.PushTransform(new TranslateTransform(-x, -y)); dc.DrawDrawing(drawing);
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); return bitmap;
    }
}
