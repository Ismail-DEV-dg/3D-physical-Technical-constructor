using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class BoardDetailDrawing
{
    public const double PinLabelSize = 1.85;
    private static readonly Brush Gold = PhysicalDrawing.Ink("#BD9A52");
    private static readonly Brush ChipBody = PhysicalDrawing.Ink("#202630");

    public static void Pcb(DrawingContext dc, Rect bounds, string color)
    {
        dc.DrawRoundedRectangle(new LinearGradientBrush(PhysicalDrawing.Ink(color).Color,
            PhysicalDrawing.Ink("#122C39").Color, 70), new Pen(PhysicalDrawing.Ink("#101D26"), .25), bounds, .8, .8);
    }

    public static void Chip(DrawingContext dc, Rect bounds, string label)
    {
        const double terminalPitch = 1;
        for (var y = bounds.Top + .5; y < bounds.Bottom; y += terminalPitch)
        {
            dc.DrawRectangle(PhysicalDrawing.Metal, null, new Rect(bounds.Left - .5, y, .65, .4));
            dc.DrawRectangle(PhysicalDrawing.Metal, null, new Rect(bounds.Right - .15, y, .65, .4));
        }
        dc.DrawRoundedRectangle(ChipBody, new Pen(Brushes.Black, .15), bounds, .25, .25);
        dc.DrawEllipse(Brushes.Gray, null, new Point(bounds.Left + .6, bounds.Top + .6), .2, .2);
        TextCentered(dc, label, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2 - .6, 1, Brushes.LightGray);
    }

    public static void Usb(DrawingContext dc, Rect bounds)
    {
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.DimGray, .2), bounds, .5, .5);
        dc.DrawRectangle(PhysicalDrawing.Ink("#242B36"), new Pen(Brushes.Black, .12),
            new Rect(bounds.Left + .7, bounds.Top + .3, bounds.Width - 1.4, 1.2));
        for (var index = 0; index < 5; index++)
            dc.DrawRectangle(Gold, null, new Rect(bounds.Left + 1.2 + index * (bounds.Width - 2.4) / 5,
                bounds.Top + .65, .28, .4));
    }

    public static void Pad(DrawingContext dc, ContactDefinition contact)
    {
        var point = new Point(contact.X, contact.Y);
        dc.DrawEllipse(Gold, new Pen(PhysicalDrawing.Ink("#433C2B"), .1), point, .8, .8);
        dc.DrawRectangle(PhysicalDrawing.Ink("#333B46"), new Pen(Brushes.Black, .1),
            new Rect(point.X - .32, point.Y - .32, .64, .64));
        dc.DrawLine(new Pen(Brushes.LightGray, .12), new Point(point.X - .2, point.Y - .2),
            new Point(point.X + .2, point.Y - .2));
    }

    public static void Hole(DrawingContext dc, Point point, double radius = 1.1)
    {
        dc.DrawEllipse(PhysicalDrawing.Ink("#B8AA79"), null, point, radius + .35, radius + .35);
        dc.DrawEllipse(PhysicalDrawing.Ink("#15242E"), new Pen(Brushes.DimGray, .12), point, radius, radius);
    }

    public static void TextCentered(DrawingContext dc, string text, double centerX, double y, double size, Brush color)
    {
        var formatted = WpfTextMeasurer.Format(text, size, color);
        dc.DrawText(formatted, new Point(centerX - formatted.WidthIncludingTrailingWhitespace / 2, y));
    }

    public static string PinLabel(string id) => id switch
    {
        "GND2" => "GND", "RESET" => "RST", "AREF" => "REF",
        "GPIO36" => "VP", "GPIO39" => "VN", "GPIO1" => "TX", "GPIO3" => "RX",
        _ => id.StartsWith("GPIO", StringComparison.Ordinal) ? id[4..] : id
    };

    public static void ExternalPinLabel(DrawingContext dc, string text, double centerX, double y)
    {
        const double padding = .14;
        var formatted = WpfTextMeasurer.Format(text, PinLabelSize, PhysicalDrawing.Ink("#25303C"));
        var left = centerX - formatted.WidthIncludingTrailingWhitespace / 2;
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#DAE0E6"), new Pen(PhysicalDrawing.Ink("#3C4755"), .08),
            new Rect(left - padding, y - padding, formatted.WidthIncludingTrailingWhitespace + 2 * padding,
                formatted.Height + 2 * padding), .2, .2);
        dc.DrawText(formatted, new Point(left, y));
    }

    public static void RowLabels(DrawingContext dc, FootprintDefinition footprint, bool esp32)
    {
        var right = footprint.Contacts.Max(contact => contact.X);
        foreach (var contact in footprint.Contacts)
        {
            Pad(dc, contact);
            var label = esp32 && contact.ElectricalPinId == "5V" ? "VIN" : PinLabel(contact.ElectricalPinId);
            var special = contact.ElectricalPinId is "GPIO34" or "GPIO35" or "GPIO36" or "GPIO39";
            var ink = special ? Brushes.LightSkyBlue : Brushes.WhiteSmoke;
            if (contact.X < right / 2)
                PhysicalDrawing.Text(dc, label, contact.X + 1.35, contact.Y - 1.2, PinLabelSize, ink);
            else PhysicalDrawing.TextRight(dc, label, contact.X - 1.35, contact.Y - 1.2, PinLabelSize, ink);
        }
    }
}
