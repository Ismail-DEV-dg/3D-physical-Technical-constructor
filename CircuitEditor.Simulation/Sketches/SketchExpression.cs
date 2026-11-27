namespace CircuitEditor.Simulation.Sketches;

internal sealed record SketchExpression(SketchExpressionKind Kind, long Value = 0, string Pin = "",
    SketchExpression? Left = null, SketchExpression? Right = null, SketchExpression? Otherwise = null,
    IReadOnlyList<SketchExpression>? Arguments = null);
