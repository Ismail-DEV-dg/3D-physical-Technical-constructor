using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.App.Controls;

internal static class RealComponentDrawing
{
    public const double ReferenceFontSizeMm = 1.5;
    public static void DrawBody(DrawingContext dc, ComponentViewModel item, ComponentPlacement pose, double pitch)
    {
        var footprint = item.Footprint;
        var body = ContactGeometry.Body(footprint, pose, pitch);
        var neutral = new ComponentPlacement(spanHoles: pose.SpanHoles);
        var contacts = footprint.Contacts.Select(contact => ContactGeometry.Local(footprint, contact, neutral, pitch)).ToArray();
        if (footprint.IsFlexible)
        {
            foreach (var contact in contacts)
                PhysicalDrawing.Lead(dc,
                    new Point(contact.X, contact.Y), new Point(body.X + body.Width / 2, body.Y + body.Height / 2));
        }
        else if (footprint.Renderer is "button" or "placeholder")
        {
            foreach (var contact in contacts)
                PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y),
                    new Point(body.X + body.Width / 2, body.Y + body.Height / 2));
        }
        switch (footprint.Renderer)
        {
            case ChipPackage.Renderer: ChipPackageDrawing.Draw(dc, footprint, item.Name); break;
            case "arduino-uno": ArduinoUnoDrawing.Draw(dc, footprint); break;
            case "arduino-nano": ArduinoNanoDrawing.Draw(dc, footprint); break;
            case "esp32-devkit": Esp32Drawing.Draw(dc, footprint); break;
            case "lcd1602":
            case "lcd1602-i2c": Lcd1602Drawing.Draw(dc, footprint); break;
            case "oled-ssd1306": OledDrawing.Draw(dc, footprint); break;
            case "seven-segment": SevenSegmentDrawing.Draw(dc, footprint); break;
            case "buzzer": ActuatorBodyDrawing.Buzzer(dc, footprint); break;
            case "relay": ActuatorBodyDrawing.Relay(dc, footprint); break;
            case "servo": ServoDrawing.Draw(dc, footprint); break;
            case "capacitor": PassiveBodyDrawing.Capacitor(dc, item); break;
            case "inductor": PassiveBodyDrawing.Inductor(dc, PhysicalDrawing.Rect(body), item.Model.Value); break;
            case "diode": PassiveBodyDrawing.Diode(dc, PhysicalDrawing.Rect(body)); break;
            case "potentiometer": PotentiometerDrawing.DrawBase(dc, item); break;
            case "npn": NpnDrawing.Draw(dc, footprint); break;
            case "dht11":
            case "dht22": HumiditySensorDrawing.Draw(dc, footprint); break;
            case "hc-sr04": ProximitySensorDrawing.Ultrasound(dc, footprint); break;
            case "pir": ProximitySensorDrawing.Pir(dc, footprint); break;
            case "photoresistor": EnvironmentPassiveDrawing.Draw(dc, PhysicalDrawing.Rect(body), false); break;
            case "thermistor": EnvironmentPassiveDrawing.Draw(dc, PhysicalDrawing.Rect(body), true); break;
            case "rgb-led": RgbLedDrawing.Draw(dc, item); break;
            case "switch": MiscComponentDrawing.Switch(dc, item); break;
            case "battery": MiscComponentDrawing.Battery(dc, item); break;
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
                LedDrawing.Draw(dc, item, body, contacts);
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
        if (footprint.Renderer is "axial-resistor" or "inductor" or "diode" or "photoresistor" or "thermistor" or "led" or "button" or "placeholder")
            foreach (var contact in contacts)
                PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
    }

    public static void DrawSelection(DrawingContext dc, ComponentViewModel item, ComponentPlacement pose,
        double pitch, bool invalidPreview, double? simulatedAngle = null)
    {
        if (item.Footprint.Renderer == "potentiometer") PotentiometerDrawing.DrawKnob(dc, item, simulatedAngle);
        if (item.Footprint.Renderer == "servo") ServoDrawing.DrawArm(dc, item.Footprint, simulatedAngle ?? 0);
        if (item.IsSelected || invalidPreview)
        {
            var body = ContactGeometry.Body(item.Footprint, pose, pitch);
            var outline = PhysicalDrawing.Ink(invalidPreview ? "#FF3B5C" : "#70D9EE");
            dc.DrawRoundedRectangle(null, new Pen(outline, .35), PhysicalDrawing.Rect(body), .7, .7);
            var label = item.Footprint.Renderer == "axial-resistor" ? $"{item.Reference}  {item.Model.Value}" : item.Reference;
            PhysicalDrawing.Text(dc, label, body.X, body.Y - 2.5, ReferenceFontSizeMm, outline);
            if (item.Footprint.IsFlexible)
            {
                var localPose = new ComponentPlacement(spanHoles: pose.SpanHoles);
                var pinPen = new Pen(outline, .25);
                foreach (var contact in item.Footprint.Contacts)
                {
                    var pin = ContactGeometry.Local(item.Footprint, contact, localPose, pitch);
                    dc.DrawRectangle(null, pinPen, new Rect(pin.X - .55, pin.Y - .55, 1.1, 1.1));
                }
            }
        }
    }
}
