using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class PotentiometerDrawing
{
    public static void DrawBase(DrawingContext dc, ComponentViewModel item)
    {
        var body = PhysicalDrawing.Rect(item.Footprint.Body);
        foreach (var contact in item.Footprint.Contacts)
            PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y), new Point(contact.X, body.Top + body.Height / 2));
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#33685F"), new Pen(PhysicalDrawing.Ink("#163B35"), .18), body, .9, .9);
        BoardDetailDrawing.TextCentered(dc, item.Model.Value, body.Left + body.Width / 2, body.Top + .15, 1.1, Brushes.WhiteSmoke);
        foreach (var contact in item.Footprint.Contacts)
        {
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
            BoardDetailDrawing.ExternalPinLabel(dc, contact.ElectricalPinId, contact.X, .6);
        }
    }

    public static void DrawKnob(DrawingContext dc, ComponentViewModel item, double? angleOverride = null)
    {
        if (item.Footprint.RotaryControl is not { } knob) return;
        var center = new Point(knob.X, knob.Y);
        dc.DrawEllipse(PhysicalDrawing.Metal, new Pen(Brushes.Black, .2), center, knob.Radius, knob.Radius);
        dc.DrawEllipse(PhysicalDrawing.Ink("#252D39"), new Pen(Brushes.DimGray, .14), center, knob.Radius * .83, knob.Radius * .83);
        var angle = (angleOverride ?? item.Model.PotentiometerAngle) * Math.PI / 180;
        dc.DrawLine(new Pen(Brushes.WhiteSmoke, .45), center,
            new Point(center.X + Math.Sin(angle) * knob.Radius * .7, center.Y - Math.Cos(angle) * knob.Radius * .7));
    }
}
