using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.Controls;

internal sealed class WireDrawingCache
{
    private readonly ConditionalWeakTable<WirePath, StreamGeometry> geometries = new();

    public StreamGeometry Get(WirePath path) => geometries.GetValue(path, static path => Build(path.Points));

    public static StreamGeometry Build(IReadOnlyList<LayoutPoint> points)
    {
        StreamGeometry geometry = new();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(points[0].X, points[0].Y), false, false);
            var rest = new Point[points.Count - 1];
            for (var i = 1; i < points.Count; i++) rest[i - 1] = new(points[i].X, points[i].Y);
            context.PolyLineTo(rest, true, false);
        }
        geometry.Freeze();
        return geometry;
    }
}
