namespace CircuitEditor.Core.Scene;

/// <summary>Минимум и максимум сцены по каждой оси.</summary>
public readonly record struct SceneBounds(ScenePoint Min, ScenePoint Max)
{
    /// <summary>Замкнутая область, содержащая переданные точки.</summary>
    public static SceneBounds From(IEnumerable<ScenePoint> points)
    {
        using var iterator = points.GetEnumerator();
        if (!iterator.MoveNext()) return default;
        var min = iterator.Current;
        var max = min;
        while (iterator.MoveNext())
        {
            var p = iterator.Current;
            min = new(Math.Min(min.X, p.X), Math.Min(min.Y, p.Y), Math.Min(min.Z, p.Z));
            max = new(Math.Max(max.X, p.X), Math.Max(max.Y, p.Y), Math.Max(max.Z, p.Z));
        }
        return new(min, max);
    }
}
