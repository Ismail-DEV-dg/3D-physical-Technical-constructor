using System.Collections.Immutable;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Core.Scene;

/// <summary>Превращает плоскую трассу «Макета» в арку с неизменяемыми физическими концами.</summary>
public static class SceneWirePath
{
    /// <summary>Поднимает выборку по длине дуги и сохраняет начальную и конечную высоты.</summary>
    public static ImmutableArray<ScenePoint> Create(Wire wire, ScenePoint start, ScenePoint end, SceneWireProfile style)
    {
        var route = WireCurve.Sample([new LayoutPoint(start.X, start.Y), .. wire.ControlPoints, new(end.X, end.Y)]);
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
        var points = ImmutableArray.CreateBuilder<ScenePoint>(route.Count);
        points.Add(start);
        for (var i = 1; i < route.Count - 1; i++)
        {
            var t = lengths[i] / length;
            var p = route[i];
            points.Add(new(p.X, p.Y, start.Z + (end.Z - start.Z) * t + rise * Math.Sin(Math.PI * t)));
        }
        points.Add(end);
        return points.MoveToImmutable();
    }
}
