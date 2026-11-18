using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class SensorPinDrawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        foreach (var contact in footprint.Contacts)
        {
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
            var text = WpfTextMeasurer.Format(contact.ElectricalPinId, BoardDetailDrawing.PinLabelSize,
                contact.ElectricalPinId == "NC" ? Brushes.DimGray : PhysicalDrawing.Ink("#243440"));
            dc.PushTransform(new TranslateTransform(contact.X + text.Height / 2, contact.Y + .7));
            dc.PushTransform(new RotateTransform(90));
            dc.DrawRectangle(PhysicalDrawing.Ink("#DEE4EA"), null, new Rect(0, 0, text.WidthIncludingTrailingWhitespace, text.Height));
            dc.DrawText(text, new Point());
            dc.Pop(); dc.Pop();
        }
    }

    public static void StraightLeads(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = footprint.Body;
        foreach (var contact in footprint.Contacts)
            PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y), new Point(contact.X, body.Bottom - .8));
    }
}
