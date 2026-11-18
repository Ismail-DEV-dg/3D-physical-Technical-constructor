namespace CircuitEditor.Core.Physical;

/// <summary>Механическое положение потенциометра, без симуляции и типов UI.</summary>
public static class PotentiometerPosition
{
    /// <summary>Левый механический упор, градусы относительно вертикали вверх.</summary>
    public const double MinimumAngle = -135;
    /// <summary>Правый механический упор.</summary>
    public const double MaximumAngle = 135;
    private const double MinimumPointerDistanceSquared = .01;
    /// <summary>Положение движка в процентах от полного диапазона.</summary>
    public static double ToPercent(double angle) => (angle - MinimumAngle) / (MaximumAngle - MinimumAngle) * 100;
    /// <summary>Угол из процентов.</summary>
    public static double FromPercent(double percent) => MinimumAngle + percent / 100 * (MaximumAngle - MinimumAngle);
    /// <summary>Угол по указателю; в мёртвой зоне сохраняет ближайший к прежнему положению упор.</summary>
    public static double FromPointer(double dx, double dy, double previousAngle)
    {
        if (dx * dx + dy * dy < MinimumPointerDistanceSquared || !double.IsFinite(dx) || !double.IsFinite(dy)) return previousAngle;
        var angle = Math.Atan2(dx, -dy) * 180 / Math.PI;
        return Math.Abs(angle) <= MaximumAngle ? angle : previousAngle < 0 ? MinimumAngle : MaximumAngle;
    }
}
