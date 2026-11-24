using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class PassiveBodyDrawing
{
    public static void Capacitor(DrawingContext dc, ComponentViewModel item) =>
        Capacitor(dc, item.Footprint, item.Model.CapacitorKind, item.Model.Value);

    internal static void Capacitor(DrawingContext dc, FootprintDefinition footprint, CapacitorKind kind, string value)
    {
        var electrolytic = kind == CapacitorKind.Electrolytic;
        var body = PhysicalDrawing.Rect(footprint.Parts![electrolytic ? "electrolytic" : "ceramic"]);
        foreach (var contact in footprint.Contacts)
            PhysicalDrawing.Lead(dc, new Point(contact.X, contact.Y), new Point(contact.X, body.Top + body.Height / 2));
        var center = new Point(body.Left + body.Width / 2, body.Top + body.Height / 2);
        if (electrolytic)
        {
            dc.DrawEllipse(new LinearGradientBrush(PhysicalDrawing.Ink("#202C40").Color,
                PhysicalDrawing.Ink("#526883").Color, 0), new Pen(Brushes.Black, .18), center, body.Width / 2, body.Height / 2);
            dc.DrawEllipse(PhysicalDrawing.Metal, new Pen(Brushes.DimGray, .15),
                new Point(center.X, center.Y - .5), body.Width * .4, body.Height * .31);
            dc.DrawLine(new Pen(Brushes.DimGray, .12), new Point(center.X - 1, center.Y - .5), new Point(center.X + 1, center.Y - .5));
            dc.DrawLine(new Pen(Brushes.DimGray, .12), new Point(center.X, center.Y - 1.4), new Point(center.X, center.Y + .4));
            dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#D0D4D2"), null,
                new Rect(body.Right - 1.3, center.Y - 1.3, .9, 3), .25, .25);
            PhysicalDrawing.Text(dc, "−", body.Right - 1.25, center.Y, 1.2, Brushes.Black);
            BoardDetailDrawing.TextCentered(dc, value, center.X - .45, body.Bottom - 1.7, 1, Brushes.WhiteSmoke);
            PhysicalDrawing.Text(dc, "+", footprint.Contacts[0].X - 1.4, -.8, 1.5, Brushes.OrangeRed);
        }
        else
        {
            dc.DrawEllipse(new LinearGradientBrush(PhysicalDrawing.Ink("#E9B064").Color,
                PhysicalDrawing.Ink("#A3572F").Color, 70), new Pen(PhysicalDrawing.Ink("#7B4024"), .16), center, body.Width / 2, body.Height / 2);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 255, 236, 185)), null,
                new Point(center.X - 1.1, center.Y - 1.3), 1.1, .65);
            BoardDetailDrawing.TextCentered(dc, value, center.X, center.Y + .4, 1.35, PhysicalDrawing.Ink("#3A231A"));
        }
        foreach (var contact in footprint.Contacts) PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
    }

    public static void Inductor(DrawingContext dc, Rect body, string value)
    {
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#3F474C"), new Pen(Brushes.Black, .18), body, .7, .7);
        const int turns = 12;
        for (var index = 0; index < turns; index++)
        {
            var x = body.Left + .3 + index * (body.Width - .6) / turns;
            dc.DrawEllipse(null, new Pen(PhysicalDrawing.Ink("#62371D"), .42),
                new Point(x, body.Top + body.Height / 2), .28, body.Height * .45);
            dc.DrawEllipse(null, new Pen(PhysicalDrawing.Ink("#D49856"), .2),
                new Point(x, body.Top + body.Height / 2 - .08), .18, body.Height * .42);
        }
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#E6D7B4"), null,
            new Rect(body.Left + .5, body.Top + body.Height / 2 - .7, body.Width - 1, 1.65), .2, .2);
        BoardDetailDrawing.TextCentered(dc, value, body.Left + body.Width / 2, body.Top + body.Height / 2 - .6, 1.05, Brushes.Black);
    }

    public static void Diode(DrawingContext dc, Rect body)
    {
        dc.DrawRoundedRectangle(new LinearGradientBrush(Colors.DimGray, Color.FromRgb(20, 25, 30), 90),
            new Pen(Brushes.Black, .15), body, .45, .45);
        dc.DrawRectangle(PhysicalDrawing.Ink("#C2C9CC"), null, new Rect(body.Right - 1.2, body.Top + .08, .7, body.Height - .16));
        BoardDetailDrawing.TextCentered(dc, "4007", body.Left + body.Width * .4, body.Top + .6, .85, Brushes.LightGray);
        BoardDetailDrawing.ExternalPinLabel(dc, "A", body.Left - 1, body.Bottom + .6);
        BoardDetailDrawing.ExternalPinLabel(dc, "K", body.Right + 1, body.Bottom + .6);
    }
}
