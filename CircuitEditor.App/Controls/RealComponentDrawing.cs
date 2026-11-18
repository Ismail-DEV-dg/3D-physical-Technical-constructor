using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.App.Controls;

internal static class RealComponentDrawing
{
    public static void Draw(DrawingContext dc, ComponentViewModel item, ComponentPlacement pose,
        double pitch, bool invalidPreview)
    {
        var footprint = item.Footprint;
        var body = ContactGeometry.Body(footprint, pose, pitch);
        dc.PushTransform(new TranslateTransform(pose.X, pose.Y));
        dc.PushTransform(new RotateTransform(pose.Rotation));
        var neutral = new ComponentPlacement(spanHoles: pose.SpanHoles);
        var contacts = footprint.Contacts.Select(contact => ContactGeometry.Local(footprint, contact, neutral, pitch)).ToArray();
        if (footprint.IsFlexible)
        {
            foreach (var contact in contacts)
                PhysicalDrawing.Lead(dc,
                    new Point(contact.X, contact.Y), new Point(body.X + body.Width / 2, body.Y + body.Height / 2));
        }
        else if (footprint.Renderer is "led" or "button" or "placeholder")
        {
            foreach (var contact in contacts)
                PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y),
                    new Point(body.X + body.Width / 2, body.Y + body.Height / 2));
        }
        switch (footprint.Renderer)
        {
            case "arduino-uno": ArduinoUnoDrawing.Draw(dc, footprint); break;
            case "arduino-nano": ArduinoNanoDrawing.Draw(dc, footprint); break;
            case "esp32-devkit": Esp32Drawing.Draw(dc, footprint); break;
            case "lcd1602":
            case "lcd1602-i2c": Lcd1602Drawing.Draw(dc, footprint); break;
            case "oled-ssd1306": OledDrawing.Draw(dc, footprint); break;
            case "seven-segment": SevenSegmentDrawing.Draw(dc, footprint); break;
            case "axial-resistor":
                dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(236, 215, 175), Color.FromRgb(189, 156, 106), 90),
                    new Pen(PhysicalDrawing.Ink("#87663F"), .15), PhysicalDrawing.Rect(body), 1, 1);
                var bands = ResistorMarking.GetBands(item.Model.Value, footprint.TolerancePercent);
                for (var index = 0; index < bands.Count; index++)
                {
                    var x = body.X + body.Width * (bands.Count == 1 ? .48 : .16 + index * .65 / (bands.Count - 1));
                    dc.DrawRectangle(PhysicalDrawing.Ink(bands[index]), null,
                        new Rect(x, body.Y + .08, body.Width * .075, body.Height - .16));
                }
                if (bands.Count == 0) PhysicalDrawing.Text(dc, "?", body.X + body.Width / 2, body.Y, 1.8, Brushes.DimGray);
                break;
            case "led":
                var color = (Color)ColorConverter.ConvertFromString(item.Model.Color);
                dc.DrawEllipse(PhysicalDrawing.Ink("#9D2139"), new Pen(PhysicalDrawing.Ink("#68202D"), .2),
                    new Point(body.X + body.Width / 2, body.Y + body.Height / 2 + .6), body.Width / 2 + .35, body.Height / 2);
                dc.DrawEllipse(new LinearGradientBrush(Colors.MistyRose, color, 45),
                    new Pen(PhysicalDrawing.Ink("#8B1830"), .15), new Point(body.X + body.Width / 2, body.Y + body.Height / 2),
                    body.Width / 2, body.Height / 2);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), null,
                    new Point(body.X + body.Width * .32, body.Y + body.Height * .3), .65, .9);
                break;
            case "button":
                dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.DimGray, .25), PhysicalDrawing.Rect(body), .4, .4);
                dc.DrawRectangle(PhysicalDrawing.Ink("#27303A"), null,
                    new Rect(body.X + .45, body.Y + .45, body.Width - .9, body.Height - .9));
                dc.DrawEllipse(PhysicalDrawing.Ink("#151B23"), new Pen(Brushes.Gray, .2),
                    new Point(body.X + body.Width / 2, body.Y + body.Height / 2), body.Width * .33, body.Height * .33);
                break;
            default:
                dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#253245"), new Pen(PhysicalDrawing.Ink("#8B5CFF"), .2),
                    PhysicalDrawing.Rect(body), .6, .6);
                if (body.Width >= 20 && body.Height >= 10)
                {
                    PhysicalDrawing.Text(dc, item.Name, body.X + 1, body.Y + 1, 1.35, Brushes.WhiteSmoke);
                    PhysicalDrawing.Text(dc, "ЗАГЛУШКА", body.X + 1, body.Bottom - 3, 1.2, Brushes.LightGray);
                }
                else PhysicalDrawing.Text(dc, "?", body.X + body.Width / 2 - .5, body.Y, 1.7, Brushes.WhiteSmoke);
                break;
        }
        if (footprint.Renderer is "axial-resistor" or "led" or "button" or "placeholder")
            foreach (var contact in contacts)
                PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
        if (item.IsSelected || invalidPreview)
        {
            var outline = PhysicalDrawing.Ink(invalidPreview ? "#FF3B5C" : "#00E5FF");
            dc.DrawRoundedRectangle(null, new Pen(outline, .35), PhysicalDrawing.Rect(body), .7, .7);
            PhysicalDrawing.Text(dc, item.Reference, body.X, body.Y - 2.5, 1.5, outline);
        }
        dc.Pop(); dc.Pop();
    }
}
