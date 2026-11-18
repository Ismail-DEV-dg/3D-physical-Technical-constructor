using CircuitEditor.Core.Placement;

namespace CircuitEditor.Core.Layout;

/// <summary>Преобразования символьной раскладки относительно её собственного положения.</summary>
public static class LayoutTransform
{
    /// <summary>Локальная точка в координаты схемы.</summary>
    public static LayoutPoint ToWorld(ComponentPlacement placement, ComponentLayout layout, LayoutPoint point)
    {
        var cx = layout.Width / 2; var cy = layout.Height / 2;
        var angle = placement.Rotation * Math.PI / 180;
        var x = point.X - cx; var y = point.Y - cy;
        return new LayoutPoint(placement.X + cx + x * Math.Cos(angle) - y * Math.Sin(angle),
            placement.Y + cy + x * Math.Sin(angle) + y * Math.Cos(angle));
    }
    /// <summary>Координаты схемы в локальные координаты символа.</summary>
    public static LayoutPoint ToLocal(ComponentPlacement placement, ComponentLayout layout, LayoutPoint point)
    {
        var cx = placement.X + layout.Width / 2; var cy = placement.Y + layout.Height / 2;
        var angle = -placement.Rotation * Math.PI / 180;
        var x = point.X - cx; var y = point.Y - cy;
        return new LayoutPoint(layout.Width / 2 + x * Math.Cos(angle) - y * Math.Sin(angle),
            layout.Height / 2 + x * Math.Sin(angle) + y * Math.Cos(angle));
    }
}
