using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Tests;

internal static class BatchAPresentationChecks
{
    private static readonly string[] Ids = ["arduino-nano", "esp32-devkit", "lcd1602", "lcd1602-i2c", "oled-ssd1306", "seven-segment"];

    public static void Run(ResourceDictionary resources)
    {
        var theme = new ThemeService(new WpfThemeResources(resources));
        foreach (var kind in new[] { ThemeKind.NeonDark, ThemeKind.LightTech })
        {
            Assert.True(theme.SetTheme(kind));
            foreach (var id in Ids) VerifyRotations(resources, id);
            SaveAtlasIfRequested(resources, kind);
        }
        Assert.True(theme.SetTheme(ThemeKind.NeonDark));
    }

    private static void VerifyRotations(ResourceDictionary resources, string id)
    {
        using var project = TestProjectFactory.Create(new WpfTextMeasurer());
        using var mockup = TestProjectFactory.Mockup(project);
        var item = project.Add(project.Library.Single(item => item.Id == id))!;
        item.IsSelected = false;
        for (var rotation = 0; rotation < 360; rotation += 90)
        {
            while (item.MockupPlacement.Rotation != rotation) item.MockupPlacement.Rotate();
            var bounds = ContactGeometry.Bounds(item.Footprint, item.MockupPlacement, mockup.Board!.Definition.HolePitch);
            item.MockupPlacement.MoveTo(5 - bounds.X, 5 - bounds.Y);
            var surface = new BreadboardSurface { Scene = mockup, Resources = resources,
                Background = (Brush)resources["BackgroundBrush"] };
            foreach (var contact in item.Footprint.Contacts)
            {
                var position = ContactGeometry.World(item.Footprint, contact, item.MockupPlacement, mockup.Board.Definition.HolePitch);
                var tip = surface.GetPinToolTip(new Point(PhysicalSettings.ToPixels(position.X) + mockup.OffsetX,
                    PhysicalSettings.ToPixels(position.Y) + mockup.OffsetY));
                Assert.Contains($"контакт {contact.Id}\n", tip);
                Assert.Contains(contact.ElectricalPinId, tip);
                if (contact.ElectricalPinId is "GPIO34" or "GPIO35" or "GPIO36" or "GPIO39") Assert.Contains("input only", tip);
                if (contact.ElectricalPinId is "RESET" or "EN") Assert.Contains("active-low", tip);
            }
            var bitmap = Render(surface, 550, 550);
            var pixelBounds = new Int32Rect(35, 35, (int)(bounds.Width * PhysicalSettings.PixelsPerMillimeter) - 2,
                (int)(bounds.Height * PhysicalSettings.PixelsPerMillimeter) - 2);
            var pixels = new int[pixelBounds.Width * pixelBounds.Height];
            bitmap.CopyPixels(pixelBounds, pixels, pixelBounds.Width * sizeof(int), 0);
            Assert.True(pixels.Distinct().Take(12).Count() >= 12, $"Пустой корпус {id}, поворот {rotation}");
        }
    }

    private static void SaveAtlasIfRequested(ResourceDictionary resources, ThemeKind theme)
    {
        var output = Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_VISUAL_CHECK");
        if (string.IsNullOrWhiteSpace(output)) return;
        using var project = TestProjectFactory.Create(new WpfTextMeasurer());
        using var mockup = TestProjectFactory.Mockup(project);
        (double X, double Y)[] positions = [(4, 15), (30, 15), (75, 5), (75, 50), (10, 80), (48, 90)];
        for (var index = 0; index < Ids.Length; index++)
        {
            var item = project.Add(project.Library.Single(item => item.Id == Ids[index]))!;
            item.MockupPlacement.MoveTo(positions[index].X, positions[index].Y);
        }
        foreach (var item in project.Components) item.IsSelected = false;
        var surface = new BreadboardSurface { Scene = mockup, Resources = resources,
            Background = (Brush)resources["BackgroundBrush"] };
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(Render(surface, 1000, 700)));
        Directory.CreateDirectory(output);
        using var file = File.Create(Path.Combine(output, $"batch-a-{theme}.png"));
        encoder.Save(file);
    }

    private static RenderTargetBitmap Render(FrameworkElement surface, int width, int height)
    {
        surface.Measure(new Size(width, height));
        surface.Arrange(new Rect(0, 0, width, height));
        surface.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        return bitmap;
    }
}
