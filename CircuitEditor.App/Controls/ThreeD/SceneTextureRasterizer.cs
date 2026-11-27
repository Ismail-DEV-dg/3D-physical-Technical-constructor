using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneTextureRasterizer
{
    private const int MaximumDimension = 1024;
    private const int Dpi = 96;

    public static Material Create(Drawing drawing, Rect region, double pixelsPerUnit)
    {
        var scale = Math.Min(pixelsPerUnit, MaximumDimension / Math.Max(region.Width, region.Height));
        var width = Math.Max(1, (int)Math.Ceiling(region.Width * scale));
        var height = Math.Max(1, (int)Math.Ceiling(region.Height * scale));
        var sx = width / region.Width;
        var sy = height / region.Height;
        var paint = new DrawingVisual();
        using (var context = paint.RenderOpen())
        {
            context.PushTransform(new MatrixTransform(sx, 0, 0, sy, -region.X * sx, -region.Y * sy));
            context.DrawDrawing(drawing);
        }
        var bitmap = new RenderTargetBitmap(width, height, Dpi, Dpi, PixelFormats.Pbgra32);
        bitmap.Render(paint);
        bitmap.Freeze();
        var material = new DiffuseMaterial(new ImageBrush(bitmap) { Stretch = Stretch.Fill });
        material.Freeze();
        return material;
    }
}
