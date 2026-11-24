using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class ChipPackageDrawing
{
    private static readonly Brush Surface = PhysicalDrawing.Frozen(PhysicalDrawing.Ink("#242934"));
    private static readonly Brush Edge = PhysicalDrawing.Frozen(PhysicalDrawing.Ink("#05080D"));
    private static readonly Brush Legend = PhysicalDrawing.Frozen(PhysicalDrawing.Ink("#DCE3ED"));

    public static void Draw(DrawingContext dc, FootprintDefinition footprint, string label)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        foreach (var contact in footprint.Contacts)
        {
            var end = new Point(contact.X, contact.Y);
            PhysicalDrawing.Lead(dc, end, new Point(contact.X < body.Left ? body.Left + .55 : body.Right - .55, contact.Y));
            PhysicalDrawing.Contact(dc, end);
        }

        dc.DrawRoundedRectangle(Surface, new Pen(Edge, .25), body, .42, .42);
        var key = new Point(body.Left + body.Width / 2, body.Top + .17);
        dc.DrawEllipse(Edge, null, key, .98, .57);
        dc.DrawEllipse(Legend, null, new Point(body.Left + .66, body.Top + 1.43), .17, .17);
        PhysicalDrawing.Text(dc, "1", body.Left + .86, body.Top + .98, 1, Legend);

        var center = new Point(body.Left + body.Width / 2, body.Top + body.Height / 2);
        var name = WpfTextMeasurer.Format(label, 1.22, Legend);
        name.MaxTextWidth = Math.Max(body.Height - 5, 2);
        name.Trimming = TextTrimming.CharacterEllipsis;
        name.MaxLineCount = 1;
        dc.PushTransform(new RotateTransform(90, center.X, center.Y));
        dc.DrawText(name, new(center.X - name.Width / 2, center.Y - name.Height / 2));
        dc.Pop();
    }
}
