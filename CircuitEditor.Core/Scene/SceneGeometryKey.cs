using System.Globalization;
using System.Text;

namespace CircuitEditor.Core.Scene;

/// <summary>Ключ локальной геометрии. Положение корпуса, поворот, выделение и состояние сеанса исключены.</summary>
public static class SceneGeometryKey
{
    private const int CoordinatePrecision = 7;

    /// <summary>Возвращает структурный ключ, одинаковый у геометрически одинаковых экземпляров.</summary>
    public static string Of(SceneNode node)
    {
        var key = new StringBuilder();
        key.Append(CultureInfo.InvariantCulture,
            $"{node.Family}|{node.Profile.Shape}|{node.Profile.Material}|{node.Profile.Height:R}|{node.Profile.Clearance:R}|{node.Profile.DetailHeight:R}|");
        Rect(node.Body);
        foreach (var (name, part) in node.Parts.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            Text(name);
            Rect(part);
        }
        foreach (var c in node.Contacts)
        {
            Text(c.Id);
            Point(c.Position);
            key.Append(c.HoleId is null ? '0' : '1');
        }
        if (node.Footprint is { } footprint)
        {
            Text(footprint.Id);
            foreach (var contact in footprint.Contacts) Text(contact.ElectricalPinId);
        }
        if (node.Breadboard is { } board)
        {
            Text(board.Id);
            key.Append(CultureInfo.InvariantCulture, $"{board.HolePitch:R}|");
            foreach (var hole in board.Holes) Text(hole.GroupId);
        }
        foreach (var pt in node.Path) Point(pt);
        Text(node.Color);
        Text(node.Variant);
        if (node.Family is "axial-resistor" or "battery" or "capacitor" or "inductor" or "potentiometer") Text(node.Value);
        if (node.Family is "dip-chip" or "unknown") Text(node.Name);
        key.Append(CultureInfo.InvariantCulture, $"{node.WireRadius:R}|{node.Tolerance:R}|{node.KnobRadius:R}|");
        if (node.Knob is { } knob) Point(new(knob.X, knob.Y, 0));
        return key.ToString();

        void Text(string? text) => key.Append((text?.Length ?? 0).ToString(CultureInfo.InvariantCulture))
            .Append(':').Append(text).Append('|');
        void Point(ScenePoint p) => key.Append(CultureInfo.InvariantCulture,
            $"{Round(p.X):R},{Round(p.Y):R},{Round(p.Z):R}|");
        void Rect(Layout.LayoutRect r) => key.Append(CultureInfo.InvariantCulture,
            $"{r.X:R},{r.Y:R},{r.Width:R},{r.Height:R}|");
    }

    private static double Round(double values)
    {
        var rounded = Math.Round(values, CoordinatePrecision);
        return rounded == 0 ? 0 : rounded;
    }
}
