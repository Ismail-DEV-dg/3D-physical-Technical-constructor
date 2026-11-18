using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class ArduinoUnoDrawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        dc.DrawRoundedRectangle(new LinearGradientBrush(PhysicalDrawing.Ink("#087A92").Color,
            PhysicalDrawing.Ink("#00566F").Color, 90), new Pen(PhysicalDrawing.Ink("#003D4C"), .35), body, 1.5, 1.5);
        foreach (var point in new[] { new Point(3, 3), new Point(body.Width - 3, 7),
            new Point(body.Width - 3, body.Height - 3), new Point(3, body.Height - 3) })
        {
            dc.DrawEllipse(PhysicalDrawing.Ink("#B9AA74"), null, point, 1.7, 1.7);
            dc.DrawEllipse(PhysicalDrawing.Ink("#1B3640"), null, point, .85, .85);
        }
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.Gray, .25), new Rect(-1, 10, 11, 13), .4, .4);
        dc.DrawRectangle(PhysicalDrawing.Ink("#30363D"), null, new Rect(-1.2, 12.2, 1.8, 8));
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#151C23"), new Pen(Brushes.Black, .3), new Rect(0, 33, 12, 10), .8, .8);
        dc.DrawEllipse(Brushes.Black, new Pen(Brushes.DimGray, .5), new Point(2, 38), 2.7, 2.7);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#1C2429"), new Pen(Brushes.Black, .2), new Rect(27, 27, 29, 8), .4, .4);
        for (var index = 0; index < 14; index++)
        {
            var x = 28 + index * 2;
            dc.DrawRectangle(PhysicalDrawing.Metal, null, new Rect(x, 25.8, .8, 1.2));
            dc.DrawRectangle(PhysicalDrawing.Metal, null, new Rect(x, 35, .8, 1.2));
        }
        PhysicalDrawing.Text(dc, "ATmega328P", 32, 29.5, 1.5, Brushes.LightGray);
        PhysicalDrawing.Text(dc, "ARDUINO", 24, 12, 3.5, Brushes.White);
        PhysicalDrawing.Text(dc, "UNO", 42, 18, 2.7, Brushes.White);
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.Gray, .2), new Rect(16, 29, 6, 3), 1, 1);
        dc.DrawEllipse(PhysicalDrawing.Ink("#283A42"), new Pen(Brushes.Silver, .2), new Point(15, 42), 2, 2);
        foreach (var contact in footprint.Contacts)
        {
            dc.DrawRectangle(PhysicalDrawing.Ink("#11171D"), new Pen(Brushes.Black, .12),
                new Rect(contact.X - 1.2, contact.Y - 1.2, 2.4, 2.4));
            dc.DrawRectangle(PhysicalDrawing.Ink("#B19A56"), null, new Rect(contact.X - .55, contact.Y - .55, 1.1, 1.1));
            dc.DrawRectangle(Brushes.Black, null, new Rect(contact.X - .3, contact.Y - .3, .6, .6));
            var top = contact.Y < body.Height / 2;
            dc.PushTransform(new TranslateTransform(contact.X + .7, top ? contact.Y + 2 : contact.Y - 2));
            dc.PushTransform(new RotateTransform(top ? 90 : -90));
            PhysicalDrawing.Text(dc, contact.ElectricalPinId, 0, 0, 1.4, Brushes.WhiteSmoke);
            dc.Pop(); dc.Pop();
        }
    }
}
