using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class ActuatorBodyDrawing
{
    public static void Buzzer(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        var center = new Point(body.Left + body.Width / 2, body.Top + body.Height / 2);
        foreach (var contact in footprint.Contacts) PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y), center);
        dc.DrawEllipse(PhysicalDrawing.Ink("#111820"), new Pen(Brushes.Black, .25), center, body.Width / 2, body.Height / 2);
        dc.DrawEllipse(new LinearGradientBrush(PhysicalDrawing.Ink("#555E69").Color,
            PhysicalDrawing.Ink("#1B232E").Color, 55), new Pen(PhysicalDrawing.Ink("#69727D"), .14),
            new Point(center.X, center.Y - .5), body.Width * .46, body.Height * .43);
        dc.DrawEllipse(Brushes.Black, new Pen(Brushes.DimGray, .2), center, 1.8, 1.8);
        PhysicalDrawing.Text(dc, "+", center.X - 3.8, center.Y - 3.8, 2.1, Brushes.WhiteSmoke);
        PhysicalDrawing.Text(dc, "−", center.X + 2, center.Y - 3.8, 2.1, Brushes.WhiteSmoke);
        BoardDetailDrawing.TextCentered(dc, "5V ACTIVE", center.X, body.Bottom - 3.5, 1.25, Brushes.LightGray);
        foreach (var contact in footprint.Contacts) PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
    }

    public static void Relay(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        foreach (var contact in footprint.Contacts)
            PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y), new Point(contact.X < body.Left ? body.Left + 1 : body.Right - 1, contact.Y));
        dc.DrawRoundedRectangle(new LinearGradientBrush(PhysicalDrawing.Ink("#2685DD").Color,
            PhysicalDrawing.Ink("#12518E").Color, 90), new Pen(PhysicalDrawing.Ink("#07386B"), .25), body, .6, .6);
        dc.DrawLine(new Pen(PhysicalDrawing.Ink("#6BB4EF"), .22), new Point(body.Left + .6, body.Top + .6), new Point(body.Right - .6, body.Top + .6));
        BoardDetailDrawing.TextCentered(dc, "5V RELAY", body.Left + body.Width / 2, body.Top + 3, 1.8, Brushes.White);
        BoardDetailDrawing.TextCentered(dc, "SPDT", body.Left + body.Width / 2, body.Top + 6.5, 1.6, Brushes.WhiteSmoke);
        foreach (var contact in footprint.Contacts)
        {
            if (contact.X < body.Left) PhysicalDrawing.Text(dc, contact.ElectricalPinId, body.Left + .6, contact.Y + .4, 1.35, Brushes.White);
            else PhysicalDrawing.TextRight(dc, contact.ElectricalPinId, body.Right - .5,
                Math.Clamp(contact.Y - 1, body.Top + .5, body.Bottom - 2.3), 1.6, Brushes.White);
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
        }
    }
}
