using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class OledDrawing
{
    internal static Rect GlassBounds(Rect body) => new(body.Left + 1, body.Top + 7, body.Width - 2, 17);

    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        BoardDetailDrawing.Pcb(dc, body, "#205997");
        foreach (var x in new[] { body.Left + 2, body.Right - 2 })
            BoardDetailDrawing.Hole(dc, new Point(x, body.Bottom - 2), .9);
        var glass = GlassBounds(body);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#070D15"), new Pen(PhysicalDrawing.Ink("#667889"), .2), glass, .35, .35);
        dc.DrawRectangle(new LinearGradientBrush(Color.FromArgb(55, 100, 145, 165), Colors.Transparent, 70), null,
            new Rect(glass.Left + .4, glass.Top + .4, glass.Width - .8, glass.Height * .35));
        var flex = new Rect(body.Left + body.Width / 2 - 3, glass.Bottom, 6, 1.5);
        dc.DrawRectangle(PhysicalDrawing.Ink("#B4863E"), null, flex);
        for (var index = 0; index < 12; index++)
            dc.DrawLine(new Pen(PhysicalDrawing.Ink("#DDD0A0"), .12),
                new Point(flex.Left + .25 + index * .5, flex.Top), new Point(flex.Left + .25 + index * .5, flex.Bottom));
        PhysicalDrawing.Text(dc, "SSD1306", body.Left + 1, body.Top + 2, 1.2, Brushes.WhiteSmoke);
        foreach (var contact in footprint.Contacts)
        {
            BoardDetailDrawing.Pad(dc, contact);
            dc.PushTransform(new TranslateTransform(contact.X + .75, contact.Y + 1.25));
            dc.PushTransform(new RotateTransform(90));
            PhysicalDrawing.Text(dc, contact.ElectricalPinId, 0, 0, BoardDetailDrawing.PinLabelSize, Brushes.WhiteSmoke);
            dc.Pop(); dc.Pop();
        }
    }
}
