namespace CircuitEditor.Simulation.Sketches;

internal sealed record SketchProgram(Guid Controller, IReadOnlyList<SketchInstruction> Setup,
    IReadOnlyList<SketchInstruction> Loop, IReadOnlyList<long> Variables);
