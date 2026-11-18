using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class Esp32Drawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        var center = body.Left + body.Width / 2;
        BoardDetailDrawing.Pcb(dc, body, "#28343B");
        var moduleLeft = center - 7.7;
        dc.DrawRectangle(PhysicalDrawing.Ink("#162321"), new Pen(Brushes.Black, .12), new Rect(moduleLeft, body.Top + .5, 15.4, 9));
        var antenna = new StreamGeometry();
        using (var path = antenna.Open())
        {
            path.BeginFigure(new Point(moduleLeft + 1, body.Top + 7), false, false);
            for (var index = 0; index < 6; index++)
            {
                var x = moduleLeft + 1 + index * 2.2;
                path.LineTo(new Point(x, body.Top + (index % 2 == 0 ? 2 : 6)), true, false);
                path.LineTo(new Point(x + 2.2, body.Top + (index % 2 == 0 ? 2 : 6)), true, false);
            }
        }
        dc.DrawGeometry(null, new Pen(PhysicalDrawing.Ink("#B89F51"), .45), antenna);
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.DimGray, .2), new Rect(moduleLeft, 2, 15.4, 22), .7, .7);
        BoardDetailDrawing.TextCentered(dc, "ESP32", center, 8, 2.6, Brushes.DarkSlateGray);
        BoardDetailDrawing.TextCentered(dc, "WROOM-32", center, 12, 1.6, Brushes.DimGray);
        BoardDetailDrawing.TextCentered(dc, "Wi-Fi / BT", center, 17, 1.3, Brushes.DimGray);
        BoardDetailDrawing.Chip(dc, new Rect(center - 2.5, 27, 5, 5), "USB");
        BoardDetailDrawing.Usb(dc, new Rect(center - 4, body.Bottom - 5.5, 8, 5.5));
        foreach (var x in new[] { center - 7, center + 7 })
        {
            dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.Gray, .12), new Rect(x - 1.6, 36.8, 3.2, 2.8), .3, .3);
            dc.DrawRectangle(PhysicalDrawing.Ink("#1A2029"), null, new Rect(x - .9, 37.3, 1.8, 1.8));
        }
        BoardDetailDrawing.TextCentered(dc, "EN", center - 7, 40, 1.2, Brushes.WhiteSmoke);
        BoardDetailDrawing.TextCentered(dc, "BOOT", center + 7, 40, 1.2, Brushes.WhiteSmoke);
        BoardDetailDrawing.RowLabels(dc, footprint, true);
    }
}
