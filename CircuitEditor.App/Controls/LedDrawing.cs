using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.Controls;

internal static class LedDrawing
{
    internal static void Draw(DrawingContext dc, ComponentViewModel item, LayoutRect body, LayoutPoint[] contacts)
    {
        var color = (Color)ColorConverter.ConvertFromString(item.Model.Color ?? item.Model.Definition.DefaultColor);
        const double flange = .35;
        var left = body.X + flange;
        var right = body.Right - flange;
        var top = body.Y;
        var shoulder = top + 2.7;
        var baseY = body.Bottom - 2.3;
        var center = (left + right) / 2;
        foreach (var contact in contacts)
        {
            var start = new Point(center + (contact.X - center) * .55, baseY);
            PhysicalDrawing.Lead(dc, start, new Point(contact.X, contact.Y));
        }

        var shape = new StreamGeometry();
        using (var path = shape.Open())
        {
            path.BeginFigure(new(left, baseY), true, true);
            path.LineTo(new(left, shoulder), true, false);
            path.BezierTo(new(left, top), new(right, top), new(right, shoulder), true, false);
            path.LineTo(new(right, baseY), true, false);
        }
        var fill = new LinearGradientBrush(
            Color.FromRgb((byte)(color.R * .47), (byte)(color.G * .47), (byte)(color.B * .47)), color, 20);
        dc.DrawGeometry(fill, new Pen(PhysicalDrawing.Ink("#331E24"), .18), shape);
        dc.DrawRoundedRectangle(fill, new Pen(PhysicalDrawing.Ink("#39282B"), .12),
            new Rect(body.X, baseY - .35, body.Width, .75), .18, .18);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#65FFFFFF"), null, new Rect(left + .65, shoulder - .15, .28, 1.8), .14, .14);
    }
}
