using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class NpnDrawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        foreach (var contact in footprint.Contacts)
            PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y), new Point(body.Left + body.Width / 2 + (contact.X - 2.54) * .5, body.Bottom - .5));
        dc.DrawEllipse(new LinearGradientBrush(Colors.DimGray, PhysicalDrawing.Ink("#161C25").Color, 90),
            new Pen(Brushes.Black, .15), new Point(body.Left + body.Width / 2, body.Top + body.Height / 2), body.Width / 2, body.Height / 2);
        dc.DrawRectangle(PhysicalDrawing.Ink("#252F3A"), null, new Rect(body.Left, body.Top + body.Height / 2, body.Width, body.Height / 2));
        BoardDetailDrawing.TextCentered(dc, "2222A", body.Left + body.Width / 2, body.Top + 2.2, 1, Brushes.LightGray);
        foreach (var contact in footprint.Contacts)
        {
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
            BoardDetailDrawing.ExternalPinLabel(dc, contact.ElectricalPinId, contact.X, .6);
        }
    }
}
