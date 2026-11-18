using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Services;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Tests;

internal static class BatchBPresentationChecks
{
    private static readonly string[] Ids = ["buzzer", "servo", "relay", "capacitor", "inductor", "diode", "potentiometer", "npn"];
    public static void Run(ResourceDictionary resources)
    {
        var theme = new ThemeService(new WpfThemeResources(resources));
        foreach (var kind in new[] { ThemeKind.NeonDark, ThemeKind.LightTech })
        {
            Assert.True(theme.SetTheme(kind));
            using var project = TestProjectFactory.Create(new WpfTextMeasurer());
            using var scene = TestProjectFactory.Mockup(project);
            var cache = new ComponentDrawingCache();
            var atlas = new DrawingGroup();
            using (var dc = atlas.Open())
            {
                dc.DrawRectangle((Brush)resources["BackgroundBrush"], null, new Rect(0, 0, 1100, 730));
                for (var index = 0; index < Ids.Length; index++)
                {
                    var item = project.Add(project.Library.Single(item => item.Id == Ids[index]))!;
                    if (item.NominalEditor is { } editor && item.Model.Definition.Id == "capacitor")
                        editor.SelectedOption = editor.Options.Single(option => option.Value == 10000000m);
                    var pose = item.MockupPlacement.Copy();
                    for (var rotation = 0; rotation < 360; rotation += 90)
                    {
                        while (pose.Rotation != rotation) pose.Rotate();
                        var bounds = ContactGeometry.Bounds(item.Footprint, pose, 2.54);
                        var group = new DrawingGroup();
                        using (var draw = group.Open())
                        {
                            draw.PushTransform(new RotateTransform(rotation));
                            draw.DrawDrawing(cache.Get(item, pose, 2.54));
                            RealComponentDrawing.DrawSelection(draw, item, pose, 2.54, false);
                        }
                        Assert.True(group.Bounds.Width > 0 && group.Bounds.Height > 0);
                        var pixels = Render(group, bounds.X - 3, bounds.Y - 3, 6, 500, 500);
                        Assert.True(pixels.Distinct().Take(16).Count() >= 16);
                    }
                    var col = index % 3; var row = index / 3;
                    BoardDetailDrawing.TextCentered(dc, item.Name, col * 360 + 175, row * 235 + 12, 14, (Brush)resources["TextBrush"]);
                    dc.PushTransform(new TranslateTransform(col * 360 + 25, row * 235 + 45));
                    var local = ContactGeometry.Bounds(item.Footprint, new(), 2.54);
                    dc.PushTransform(new ScaleTransform(5, 5));
                    dc.PushTransform(new TranslateTransform(3 - local.X, 3 - local.Y));
                    dc.DrawDrawing(cache.Get(item, new(), 2.54));
                    if (item.Model.Definition.Id == "potentiometer")
                    {
                        var cached = cache.Get(item, new(), 2.54);
                        var initial = KnobPixels(item);
                        item.PotentiometerEditor!.SetAngle(90);
                        Assert.Same(cached, cache.Get(item, new(), 2.54));
                        Assert.False(initial.SequenceEqual(KnobPixels(item)));
                        PotentiometerDrawing.DrawKnob(dc, item);
                    }
                    if (item.Model.Definition.Id == "capacitor")
                    {
                        var ceramic = cache.Get(item, new(), 2.54);
                        item.SelectedCapacitorKind = CapacitorKind.Electrolytic;
                        var electrolytic = cache.Get(item, new(), 2.54);
                        Assert.NotSame(ceramic, electrolytic);
                        dc.PushTransform(new TranslateTransform(22, 0)); dc.DrawDrawing(electrolytic); dc.Pop();
                    }
                    dc.Pop(); dc.Pop(); dc.Pop();
                }
            }
            var output = Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_VISUAL_CHECK");
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(output);
                var image = Bitmap(atlas, 0, 0, 1, 1100, 730);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
                using var file = File.Create(Path.Combine(output, $"batch-b-{kind}.png")); encoder.Save(file);
            }
        }
        Assert.True(theme.SetTheme(ThemeKind.NeonDark));
        VerifyInspector(resources);
    }
    private static int[] KnobPixels(CircuitEditor.App.ViewModels.ComponentViewModel item)
    {
        var drawing = new DrawingGroup(); using (var dc = drawing.Open()) PotentiometerDrawing.DrawKnob(dc, item);
        return Render(drawing, -3, -13, 6, 150, 150);
    }
    private static void VerifyInspector(ResourceDictionary resources)
    {
        using var project = TestProjectFactory.Create(new WpfTextMeasurer());
        var item = project.Add(project.Library.Single(item => item.Id == "capacitor"))!;
        Application.Current.Resources.MergedDictionaries.Add(resources);
        var view = new PropertiesView { DataContext = project };
        var window = new Window { Content = view, Left = -10000, Top = -10000, Width = 260, Height = 900,
            ShowActivated = false, ShowInTaskbar = false };
        window.Show();
        try
        {
            window.UpdateLayout();
            var kinds = Descendants(view).OfType<ComboBox>().Single(combo => AutomationProperties.GetName(combo) == "Корпус конденсатора");
            kinds.SelectedValue = CapacitorKind.Electrolytic;
            Assert.Equal(CapacitorKind.Electrolytic, item.Model.CapacitorKind);
            var nominal = Descendants(view).OfType<ComboBox>().Single(combo => AutomationProperties.GetName(combo) == "Номинал пассивного компонента" && combo.IsVisible);
            nominal.SelectedItem = item.NominalEditor!.Options.Single(option => option.Value == 47000000m);
            Assert.Equal("47 μF", item.Model.Value);
            Assert.DoesNotContain(Descendants(view).OfType<TextBox>(), box => box.IsVisible && AutomationProperties.GetName(box) == "Номинал");
            project.Add(project.Library.Single(item => item.Id == "potentiometer")); window.UpdateLayout();
            Assert.Contains(Descendants(view).OfType<TextBox>(), box => box.IsVisible && AutomationProperties.GetName(box) == "Положение потенциометра в процентах");
            project.Select(item); window.UpdateLayout();
            Assert.Equal(CapacitorKind.Electrolytic, kinds.SelectedValue);
            Assert.Equal("47 μF", item.Model.Value);
        }
        finally { window.Close(); }
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private static int[] Render(Drawing drawing, double x, double y, double scale, int width, int height)
    {
        var bitmap = Bitmap(drawing, x, y, scale, width, height); var pixels = new int[width * height];
        bitmap.CopyPixels(pixels, width * sizeof(int), 0); return pixels;
    }
    private static RenderTargetBitmap Bitmap(Drawing drawing, double x, double y, double scale, int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale)); dc.PushTransform(new TranslateTransform(-x, -y));
            dc.DrawDrawing(drawing);
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); return bitmap;
    }
}
