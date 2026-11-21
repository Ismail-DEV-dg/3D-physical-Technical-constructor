namespace CircuitEditor.Simulation.Sketches;

internal readonly record struct SketchInstruction(SketchOperation Operation, string Pin = "", long Value = 0,
    int Target = 0, SketchExpression? Expression = null);
