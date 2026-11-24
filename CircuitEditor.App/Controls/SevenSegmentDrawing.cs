using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class SevenSegmentDrawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#DBDEE0"), new Pen(Brushes.DimGray, .2), body, .3, .3);
        var face = FaceBounds(body);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#35373C"), new Pen(Brushes.Gray, .12), face, .2, .2);
        var center = face.Left + face.Width / 2 - .5;
        var top = face.Top + 1;
        var middle = face.Top + face.Height / 2;
        var bottom = face.Bottom - 1;
        var ink = PhysicalDrawing.Ink("#8D3F46"); // Unpowered diffused lenses, no glow or simulated digit.
        foreach (var y in new[] { top, middle, bottom })
            Segment(dc, new Point(center - 2.5, y), new Point(center + 2.5, y), ink);
        foreach (var x in new[] { center - 3.1, center + 3.1 })
        {
            Segment(dc, new Point(x, top + .8), new Point(x - .25, middle - .8), ink);
            Segment(dc, new Point(x - .25, middle + .8), new Point(x - .5, bottom - .8), ink);
        }
        dc.DrawEllipse(ink, null, new Point(face.Right - .8, bottom), .55, .55);
        foreach (var contact in footprint.Contacts)
        {
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
            var topRow = contact.Y < body.Top + body.Height / 2;
            BoardDetailDrawing.TextCentered(dc, contact.ElectricalPinId, contact.X,
                topRow ? body.Top + .05 : body.Bottom - 1.42, 1.05, Brushes.DarkSlateGray);
        }
    }

    internal static void DrawActive(DrawingContext dc, FootprintDefinition footprint, int mask)
    {
        if (mask == 0) return;
        var body = PhysicalDrawing.Rect(footprint.Body);
        var face = FaceBounds(body);
        var center = face.Left + face.Width / 2 - .5;
        var top = face.Top + 1;
        var middle = face.Top + face.Height / 2;
        var bottom = face.Bottom - 1;
        var ink = PhysicalDrawing.Ink("#FF474E");
        void Horizontal(int bit, double y)
        {
            if ((mask & (1 << bit)) != 0) Segment(dc, new(center - 2.5, y), new(center + 2.5, y), ink);
        }
        void Vertical(int bit, double x, bool upper)
        {
            if ((mask & (1 << bit)) == 0) return;
            if (upper) Segment(dc, new(x, top + .8), new(x - .25, middle - .8), ink);
            else Segment(dc, new(x - .25, middle + .8), new(x - .5, bottom - .8), ink);
        }
        Horizontal(0, top);
        Vertical(1, center + 3.1, true);
        Vertical(2, center + 3.1, false);
        Horizontal(3, bottom);
        Vertical(4, center - 3.1, false);
        Vertical(5, center - 3.1, true);
        Horizontal(6, middle);
        if ((mask & (1 << 7)) != 0) dc.DrawEllipse(ink, null, new Point(face.Right - .8, bottom), .55, .55);
    }

    internal static Rect FaceBounds(Rect body) => new(body.Left + .7, body.Top + 2.5, body.Width - 1.4, body.Height - 5);

    internal static Point[] SegmentContour(Point start, Point end)
    {

        const double halfWidth = .48;
        const double tipLength = .55;
        var along = end - start;
        along.Normalize();
        var across = new Vector(-along.Y, along.X) * halfWidth;
        return [start, start + along * tipLength + across, end - along * tipLength + across,
            end, end - along * tipLength - across, start + along * tipLength - across];
    }

    private static void Segment(DrawingContext dc, Point start, Point end, Brush ink)
    {
        var points = SegmentContour(start, end);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(start, true, true);
            path.PolyLineTo(points[1..], true, false);
        }
        dc.DrawGeometry(ink, new Pen(PhysicalDrawing.Ink("#542C34"), .1), geometry);
    }
}
