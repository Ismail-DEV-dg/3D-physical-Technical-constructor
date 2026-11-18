using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class ArduinoNanoDrawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        var center = body.Left + body.Width / 2;
        const double usbOverhangMm = 3;
        BoardDetailDrawing.Pcb(dc, new Rect(body.Left, body.Top + usbOverhangMm, body.Width, body.Height - usbOverhangMm), "#1767A2");
        BoardDetailDrawing.Usb(dc, new Rect(center - 3.8, body.Top, 7.6, 6.5));
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.Gray, .12),
            new Rect(center - 1.5, 6, 3, 3), .2, .2);
        dc.DrawEllipse(PhysicalDrawing.Ink("#D7B7A4"), new Pen(Brushes.DimGray, .1), new Point(center, 7.5), .85, .85);
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.Gray, .1), new Rect(center - 2.4, 10, 4.8, 2), .8, .8);
        BoardDetailDrawing.Chip(dc, new Rect(center - 3, 15, 6, 6), "328P");
        BoardDetailDrawing.TextCentered(dc, "NANO", center, 23, 1.9, Brushes.White);
        BoardDetailDrawing.Chip(dc, new Rect(center - 2.2, 28, 4.4, 4), "USB");
        foreach (var y in new[] { 3.5, 34.5, 37.5 })
        {
            dc.DrawRectangle(PhysicalDrawing.Metal, null, new Rect(center - 2.6, y, 1.5, .7));
            dc.DrawRectangle(PhysicalDrawing.Ink("#A3BC69"), null, new Rect(center + 1.1, y, 1.4, .7));
        }
        BoardDetailDrawing.RowLabels(dc, footprint, false);
    }
}
