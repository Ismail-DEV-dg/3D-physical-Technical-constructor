using System.Collections.Immutable;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Core.Scene;

/// <summary>Превращает плоскую трассу «Макета» в арку с неизменяемыми физическими концами.</summary>
public static class SceneWirePath
{
    /// <summary>Поднимает выборку по длине дуги и сохраняет начальную и конечную высоты.</summary>
    public static ImmutableArray<ScenePoint> Create(Wire wire, ScenePoint start, ScenePoint end, SceneWireProfile style,
        bool startSocket = false, bool endSocket = false)
    {
        LayoutPoint[] vertices = [new(start.X, start.Y), .. wire.ControlPoints, new(end.X, end.Y)];
        IReadOnlyList<LayoutPoint> route = wire.IsStraight ? StraightSamples(vertices) : WireCurve.Sample(vertices);
        var lengths = new double[route.Count];
        for (var i = 1; i < route.Count; i++)
        {
            var x = route[i].X - route[i - 1].X;
            var y = route[i].Y - route[i - 1].Y;
            lengths[i] = lengths[i - 1] + Math.Sqrt(x * x + y * y);
        }

        var length = lengths[^1];
        if (length <= 0) return start == end ? [] : [start, end];
        var rise = Math.Clamp(length * style.RisePerMillimeter, style.MinimumRise, style.MaximumRise);
        var points = ImmutableArray.CreateBuilder<ScenePoint>(route.Count + (startSocket ? 1 : 0) + (endSocket ? 1 : 0));
        var from = startSocket ? start with { Z = start.Z + SceneContactDepth.Insertion } : start;
        var to = endSocket ? end with { Z = end.Z + SceneContactDepth.Insertion } : end;
        points.Add(start);
        if (startSocket) points.Add(from);
        for (var i = 1; i < route.Count - 1; i++)
        {
            var t = lengths[i] / length;
            var p = route[i];
            points.Add(new(p.X, p.Y, from.Z + (to.Z - from.Z) * t + rise * Math.Sin(Math.PI * t)));
        }
        if (endSocket) points.Add(to);
        points.Add(end);
        return points.MoveToImmutable();
    }

    private static IReadOnlyList<LayoutPoint> StraightSamples(LayoutPoint[] vertices)
    {
        const int subdivisions = 8;
        var result = new List<LayoutPoint>();
        for (var i = 1; i < vertices.Length; i++)
        {
            var a = vertices[i - 1];
            var b = vertices[i];
            for (var j = 0; j < subdivisions; j++)
            {
                var t = j / (double)subdivisions;
                result.Add(new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t));
            }
        }
        result.Add(vertices[^1]);
        return result;
    }
}
