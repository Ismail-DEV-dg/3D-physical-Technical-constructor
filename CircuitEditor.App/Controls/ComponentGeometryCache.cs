using System.Runtime.CompilerServices;
using System.Windows.Media;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.App.Controls;

internal sealed class ComponentGeometryCache
{
    private readonly ConditionalWeakTable<ComponentPlacement, ComponentDrawingGeometry> poses = new();

    public ComponentDrawingGeometry Get(FootprintDefinition footprint, ComponentPlacement placement, double pitch)
    {
        if (poses.TryGetValue(placement, out var old) && old.X == placement.X && old.Y == placement.Y &&
            old.Footprint == footprint && old.Pitch == pitch && old.Rotation == placement.Rotation)
            return old;

        var matrix = Matrix.Identity;
        matrix.Rotate(placement.Rotation);
        matrix.Translate(placement.X, placement.Y);
        var transform = new MatrixTransform(matrix);
        transform.Freeze();
        var bounds = ContactGeometry.Bounds(footprint, placement, pitch);
        var result = new ComponentDrawingGeometry(placement.X, placement.Y, placement.Rotation, pitch, footprint,
            new LayoutRect(placement.X + bounds.X, placement.Y + bounds.Y, bounds.Width, bounds.Height), transform);
        poses.Remove(placement);
        poses.Add(placement, result);
        return result;
    }
}
