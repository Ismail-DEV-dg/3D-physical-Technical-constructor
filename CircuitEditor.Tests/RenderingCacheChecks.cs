using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Tests;

internal static class RenderingCacheChecks
{
    public static void Run(ResourceDictionary resources)
    {
        VerifyBodyReuseAndInvalidation();
        VerifyBoardHoleStates();
        VerifyCullingAndReentry(resources);
    }

    private static void VerifyBodyReuseAndInvalidation()
    {
        using var project = TestProjectFactory.Create(new WpfTextMeasurer());
        var cache = new ComponentDrawingCache();
        var lcd = project.Add(project.Library.Single(item => item.Id == "lcd1602"))!;
        var original = cache.Get(lcd, lcd.MockupPlacement, 2.54);
        Assert.True(original.IsFrozen);
        var matrix = Assert.Single(Flatten(original).OfType<ImageDrawing>());
        Assert.True(Assert.IsAssignableFrom<BitmapSource>(matrix.ImageSource).IsFrozen);
        Assert.True(Flatten(original).OfType<GeometryDrawing>().Count() < 100, "Матрица LCD снова разбита на сотни Drawing-объектов.");
        lcd.MockupPlacement.MoveTo(100, 100); lcd.MockupPlacement.Rotate(); lcd.IsSelected = false;
        Assert.Same(original, cache.Get(lcd, lcd.MockupPlacement, 2.54));
        var secondLcd = project.Add(lcd.Model.Definition)!;
        Assert.Same(original, cache.Get(secondLcd, secondLcd.MockupPlacement, 2.54));

        var resistor = project.Add(project.Library.Single(item => item.Id == "resistor"))!;
        var before = cache.Get(resistor, resistor.MockupPlacement, 2.54);
        resistor.ResistorEditor!.SelectedOption = resistor.ResistorEditor.Options.Single(option => option.Ohms == 4700m);
        var after = cache.Get(resistor, resistor.MockupPlacement, 2.54);
        Assert.NotSame(before, after);
        Assert.False(Pixels(before).SequenceEqual(Pixels(after)));
        var wider = cache.Get(resistor, new ComponentPlacement(spanHoles: 8), 2.54);
        Assert.True(wider.Bounds.Width > after.Bounds.Width);

        var led = project.Add(project.Library.Single(item => item.Id == "led"))!;
        before = cache.Get(led, led.MockupPlacement, 2.54);
        Assert.True(led.Model.TryUpdateProperties(led.Model.Value, "#39FF88", null, out _));
        after = cache.Get(led, led.MockupPlacement, 2.54);
        Assert.NotSame(before, after);
        Assert.False(Pixels(before).SequenceEqual(Pixels(after)));
        cache.Clear();
        Assert.NotSame(original, cache.Get(lcd, lcd.MockupPlacement, 2.54));
    }

    private static void VerifyBoardHoleStates()
    {
        using var project = TestProjectFactory.Create(new WpfTextMeasurer());
        using var scene = TestProjectFactory.Mockup(project);
        var cache = new BreadboardDrawingCache();
        var board = scene.Board!;
        int[] Draw()
        {
            var group = new DrawingGroup();
            using (var dc = group.Open()) cache.Draw(dc, board, scene);
            return Pixels(group, .7, -board.X + 2, -board.Y + 2);
        }
        var unselected = Draw();
        var resistor = project.Add(project.Library.Single(item => item.Id == "resistor"))!;
        var available = Draw();
        Assert.False(unselected.SequenceEqual(available));
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/D/05");
        Assert.True(scene.TryPlace(resistor, new ComponentPlacement(board.X + hole.Position.X,
            board.Y + hole.Position.Y, spanHoles: 4)));
        var occupied = Draw();
        Assert.False(available.SequenceEqual(occupied));
        project.Model.Detach(resistor.Model.Id); project.Refresh();
        Assert.Equal(available, Draw());
    }

    private static void VerifyCullingAndReentry(ResourceDictionary resources)
    {
        using var project = TestProjectFactory.Create(new WpfTextMeasurer());
        using var scene = TestProjectFactory.Mockup(project);
        var lcd = project.Add(project.Library.Single(item => item.Id == "lcd1602"))!;
        lcd.IsSelected = false;
        lcd.MockupPlacement.MoveTo(10000, 10000);
        var surface = new BreadboardSurface { Scene = scene, Resources = resources, Background = Brushes.Black };
        surface.Measure(new Size(500, 500)); surface.Arrange(new Rect(0, 0, 500, 500)); surface.UpdateLayout();
        bool HasLcd() => Flatten(VisualTreeHelper.GetDrawing(surface)).OfType<GeometryDrawing>().Any(drawing =>
            drawing.Brush is LinearGradientBrush brush && brush.GradientStops.Any(stop => stop.Color == Color.FromRgb(48, 78, 197)));
        Assert.False(HasLcd());
        lcd.MockupPlacement.MoveTo(15, 10); project.Refresh();
        surface.InvalidateVisual(); surface.UpdateLayout();
        Assert.True(HasLcd());
        scene.OffsetX = -10000;
        surface.InvalidateVisual(); surface.UpdateLayout();
        Assert.False(HasLcd());
        scene.OffsetX = 16;
        surface.InvalidateVisual(); surface.UpdateLayout();
        Assert.True(HasLcd());
    }

    private static IEnumerable<Drawing> Flatten(Drawing drawing)
    {
        yield return drawing;
        if (drawing is DrawingGroup group)
            foreach (var child in group.Children)
                foreach (var nested in Flatten(child)) yield return nested;
    }

    private static int[] Pixels(Drawing drawing, double scale = 6, double x = 5, double y = 12)
    {
        const int size = 550;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.PushTransform(new TranslateTransform(x, y));
            dc.DrawDrawing(drawing);
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new int[size * size];
        bitmap.CopyPixels(pixels, size * sizeof(int), 0);
        return pixels;
    }
}
