using CircuitEditor.Core.Layout;
namespace CircuitEditor.Core.Wiring;
/// <summary>Гладкая интерполяция узлов, общая для рисования и попадания мышью.</summary>
public static class WireCurve
{
    /// <summary>Число отрезков на один участок.</summary>
    public const int SegmentsPerSpan = 12;
    /// <summary>Индекс участка кривой под указателем; совпадает с позицией вставки нового управляющего узла.</summary>
    public static int FindInsertionIndex(IReadOnlyList<LayoutPoint> points, LayoutPoint pointer, bool isStraight = false)
    {
        if (points.Count < 2) throw new ArgumentException("Для кривой нужны два конца.", nameof(points));
        var sampled = Sample(points, isStraight);
        var nearestDistance = double.PositiveInfinity;
        var nearestSegment = 0;
        for (var i = 0; i < sampled.Count - 1; i++)
        {
            var a = sampled[i]; var b = sampled[i + 1];
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            var lengthSquared = dx * dx + dy * dy;
            var t = lengthSquared == 0 ? 0 : Math.Clamp(
                ((pointer.X - a.X) * dx + (pointer.Y - a.Y) * dy) / lengthSquared, 0, 1);
            var offsetX = pointer.X - (a.X + t * dx);
            var offsetY = pointer.Y - (a.Y + t * dy);
            var distance = offsetX * offsetX + offsetY * offsetY;
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearestSegment = i;
        }
        return isStraight ? nearestSegment : nearestSegment / SegmentsPerSpan;
    }

    /// <summary>Катмулл—Ром без изменения концов и электрической связности.</summary>
    public static IReadOnlyList<LayoutPoint> Sample(IReadOnlyList<LayoutPoint> points, bool isStraight = false)
    {
        if (isStraight || points.Count < 2) return points;
        var result = new List<LayoutPoint>();
        for (var i = 0; i < points.Count - 1; i++)
        {
            var a = points[Math.Max(0,i-1)]; var b = points[i]; var c = points[i+1]; var d = points[Math.Min(points.Count-1,i+2)];
            for (var step = 0; step < SegmentsPerSpan; step++)
            {
                var t = step / (double)SegmentsPerSpan;
                static double Interpolate(double a, double b, double c, double d, double t) =>
                    .5 * (2*b + (-a+c)*t + (2*a-5*b+4*c-d)*t*t + (-a+3*b-3*c+d)*t*t*t);
                result.Add(new(Interpolate(a.X,b.X,c.X,d.X,t), Interpolate(a.Y,b.Y,c.Y,d.Y,t)));
            }
        }
        result.Add(points[^1]); return result;
    }
}
