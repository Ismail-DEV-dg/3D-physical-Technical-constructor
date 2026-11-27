using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneSurfaceTexture
{
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

    public Material ForRegion(LayoutRect region)
    {
        // WPF maps TileBrush bounds to each mesh. Sampling the crop in the brush keeps the face UVs normalized.
        var brush = new DrawingBrush(drawing)
        {
            Viewbox = PhysicalDrawing.Rect(region), ViewboxUnits = BrushMappingMode.Absolute, Stretch = Stretch.Fill
        };
        RenderOptions.SetCachingHint(brush, CachingHint.Cache);
        RenderOptions.SetCacheInvalidationThresholdMinimum(brush, .5);
        RenderOptions.SetCacheInvalidationThresholdMaximum(brush, 2);
        var material = new DiffuseMaterial(brush);
        material.Freeze();
        return material;
    }
}
