using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.App.ViewModels;

internal sealed class WirePath
{
    private readonly LayoutPoint[] terminals;
    private readonly bool isStraight;

    public WirePath(LayoutPoint[] terminals, bool isStraight)
    {
        this.terminals = terminals;
        this.isStraight = isStraight;
        Points = WireCurve.Sample(terminals, isStraight);
        Bounds = GetBounds(Points);
    }

    public IReadOnlyList<LayoutPoint> Terminals => terminals;
    public IReadOnlyList<LayoutPoint> Points { get; }
    public LayoutRect Bounds { get; }

    private static LayoutRect GetBounds(IReadOnlyList<LayoutPoint> points)
    {
        var minX = points[0].X;
        var maxX = minX;
        var minY = points[0].Y;
        var maxY = minY;
        foreach (var point in points)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }
        return new(minX, minY, maxX - minX, maxY - minY);
    }

    public WirePath WithEnds(LayoutPoint? start, LayoutPoint? end)
    {
        var first = start ?? terminals[0];
        var last = end ?? terminals[^1];
        if (first == terminals[0] && last == terminals[^1]) return this;
        var moved = (LayoutPoint[])terminals.Clone();
        moved[0] = first;
        moved[^1] = last;
        return new(moved, isStraight);
    }
}
