using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class ScenePng
{
    private const double Dpi = 96;
    private const int MaximumDimension = 16384;
    private const long MaximumPixels = 64_000_000;

    public static byte[] Capture(Viewport3D viewport, Brush? background, double scale)
    {
        if (!double.IsFinite(scale) || scale is < .25 or > 4)
            throw new ArgumentOutOfRangeException(nameof(scale), "Выберите масштаб от 25% до 400%.");
        var width = checked((int)Math.Ceiling(viewport.ActualWidth * scale));
        var height = checked((int)Math.Ceiling(viewport.ActualHeight * scale));
        if (width <= 0 || height <= 0) throw new InvalidOperationException("Вьюпорт ещё не готов.");
        if (width > MaximumDimension || height > MaximumDimension || (long)width * height > MaximumPixels)
            throw new ArgumentException("Изображение слишком велико. Выберите меньший масштаб.");

        var target = new Rect(0, 0, width, height);
        var source = new Rect(0, 0, viewport.ActualWidth, viewport.ActualHeight);
        DrawingVisual visual = new();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(background ?? Brushes.White, null, target);
            VisualBrush view = new(viewport) { Viewbox = source, ViewboxUnits = BrushMappingMode.Absolute, Stretch = Stretch.Fill };
            context.DrawRectangle(view, null, target);
        }

        RenderTargetBitmap bitmap = new(width, height, Dpi, Dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        PngBitmapEncoder png = new();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using MemoryStream buffer = new();
        png.Save(buffer);
        return buffer.ToArray();
    }
}
