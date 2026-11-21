namespace CircuitEditor.App.Controls.ThreeD;

internal sealed record SceneBenchmarkResult(bool Simulation, bool Batched, int Components, int Wires, double Width,
    double Height, int RenderTier, double InitialMs, double SingleNodeMs, double RotationFps, long SimulationMicroseconds,
    int LitFrames, int PeakVoltageWires);
