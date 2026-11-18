using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Core.Physical;

/// <summary>Общая геометрия контактов для проверки вставки и рисовальщика.</summary>
public static class ContactGeometry
{
    /// <summary>Корпус осевой детали остаётся того же размера и центрируется между ножками.</summary>
    public static LayoutRect Body(FootprintDefinition footprint, ComponentPlacement placement, double holePitch)
    {
        var span = placement.SpanHoles == 0 ? footprint.DefaultSpan : placement.SpanHoles;
        var shift = footprint.IsFlexible ? (span - footprint.DefaultSpan) * holePitch / 2 : 0;
        return footprint.Body with { X = footprint.Body.X + shift };
    }

    /// <summary>Границы корпуса и ножек с учётом вращения, мм.</summary>
    public static LayoutRect Bounds(FootprintDefinition footprint, ComponentPlacement placement, double holePitch)
    {
        var body = Body(footprint, placement, holePitch);
        var angle = placement.Rotation * Math.PI / 180;
        LayoutPoint Rotate(double x, double y) => new(x * Math.Cos(angle) - y * Math.Sin(angle),
            x * Math.Sin(angle) + y * Math.Cos(angle));
        var points = new[] { Rotate(body.X, body.Y), Rotate(body.Right, body.Y),
            Rotate(body.X, body.Bottom), Rotate(body.Right, body.Bottom) }
            .Concat(footprint.Contacts.Select(contact => Local(footprint, contact, placement, holePitch))).ToArray();
        var left = points.Min(point => point.X); var top = points.Min(point => point.Y);
        return new LayoutRect(left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);
    }
    /// <summary>Положение контакта с учётом гибких ножек и поворота корпуса вокруг первого контакта.</summary>
    public static LayoutPoint Local(FootprintDefinition footprint, ContactDefinition contact,
        ComponentPlacement placement, double holePitch)
    {
        var span = placement.SpanHoles == 0 ? footprint.DefaultSpan : placement.SpanHoles;
        var x = footprint.IsFlexible && contact.Id == footprint.Contacts.Last().Id ? span * holePitch : contact.X;
        var radians = placement.Rotation * Math.PI / 180;
        return new LayoutPoint(x * Math.Cos(radians) - contact.Y * Math.Sin(radians),
            x * Math.Sin(radians) + contact.Y * Math.Cos(radians));
    }
    /// <summary>Координаты контакта на столе, мм.</summary>
    public static LayoutPoint World(FootprintDefinition footprint, ContactDefinition contact,
        ComponentPlacement placement, double holePitch)
    {
        var local = Local(footprint, contact, placement, holePitch);
        return new LayoutPoint(placement.X + local.X, placement.Y + local.Y);
    }
}
