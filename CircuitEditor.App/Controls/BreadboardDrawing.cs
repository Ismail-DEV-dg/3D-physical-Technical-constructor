using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Breadboards;

namespace CircuitEditor.App.Controls;

internal static class BreadboardDrawing
{
    public static void DrawBase(DrawingContext dc, BreadboardDefinition data, bool showAvailable)
    {
        dc.DrawRoundedRectangle(new LinearGradientBrush(Colors.WhiteSmoke, Color.FromRgb(220, 225, 231), 0),
            new Pen(PhysicalDrawing.Ink("#9AA5B5"), .2), new Rect(0, 0, data.Width, data.Height), 1.4, 1.4);
        var rows = data.Groups.Where(group => group.Id.Contains("/row/", StringComparison.Ordinal)).ToArray();
        var columns = data.Holes.Where(hole => hole.GroupId.Contains("/row/", StringComparison.Ordinal))
            .GroupBy(hole => hole.Id.Split('/')[1]).Select(group => group.First()).OrderBy(hole => hole.Position.X).ToArray();
        if (columns.Length > 1)
        {
            var gap = columns.Zip(columns.Skip(1), (a, b) => (a, b)).MaxBy(pair => pair.b.Position.X - pair.a.Position.X);
            var left = gap.a.Position.X + data.HolePitch / 2;
            var right = gap.b.Position.X - data.HolePitch / 2;
            dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#CFD5DC"), null, new Rect(left, 3, right - left, data.Height - 6), .5, .5);
        }
        foreach (var column in columns)
            PhysicalDrawing.Text(dc, column.Id.Split('/')[1], column.Position.X - .45, 1.3, 1.4, Brushes.DimGray);
        foreach (var group in rows)
        {
            var holes = data.Holes.Where(hole => hole.GroupId == group.Id).OrderBy(hole => hole.Position.X).ToArray();
            PhysicalDrawing.TextRight(dc, group.Id.Split('/').Last().TrimStart('0'), holes[0].Position.X - 1.3,
                holes[0].Position.Y - .65, 1.3, Brushes.Gray);
        }
        foreach (var group in data.Groups.Where(group => !group.Id.Contains("/row/", StringComparison.Ordinal)))
        {
            var holes = data.Holes.Where(hole => hole.GroupId == group.Id).ToArray();
            var positive = group.Id.Contains("positive", StringComparison.Ordinal);
            var brush = PhysicalDrawing.Ink(positive ? "#DA4154" : "#3879C5");
            var x = holes[0].Position.X - .9;
            dc.DrawLine(new Pen(brush, .22), new Point(x, holes.Min(hole => hole.Position.Y) - 2),
                new Point(x, holes.Max(hole => hole.Position.Y) + 2));
            PhysicalDrawing.Text(dc, positive ? "+" : "−", x - .5, 3.2, 1.7, brush);
        }
        var ring = showAvailable ? "#75B68D" : "#A2ABB6";
        var ringPen = new Pen(PhysicalDrawing.Ink(ring), .17);
        var surround = PhysicalDrawing.Ink("#CBD0D6");
        var inside = PhysicalDrawing.Ink("#46515C");
        foreach (var hole in data.Holes)
        {
            var point = new Point(hole.Position.X, hole.Position.Y);
            dc.DrawEllipse(surround, ringPen, point, .65, .65);
            dc.DrawRoundedRectangle(inside, null,
                new Rect(point.X - .3, point.Y - .3, .6, .6), .1, .1);
        }
    }
}
