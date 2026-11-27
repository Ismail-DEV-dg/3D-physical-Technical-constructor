using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneSurfaceTexture
{
    private const double PixelsPerMillimeter = 14;
    private readonly DrawingGroup drawing = new();

    public SceneSurfaceTexture(LayoutRect bounds, Action<DrawingContext> paint)
    {
        var rect = PhysicalDrawing.Rect(bounds);
        using (var dc = drawing.Open())
        {
            dc.PushClip(new RectangleGeometry(rect));
            dc.DrawRectangle(Brushes.Transparent, null, rect);
            paint(dc);
            dc.Pop();
        }
        drawing.Freeze();
    }

    public Material ForRegion(LayoutRect region) =>
        SceneTextureRasterizer.Create(drawing, PhysicalDrawing.Rect(region), PixelsPerMillimeter);
}
