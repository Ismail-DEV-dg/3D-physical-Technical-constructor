using CircuitEditor.Core.Code;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Sketches;

public interface ISketchRunner
{
    CodeProblem? Start(SketchDocument sketch, SimulationSession session, CancellationToken cancellationToken = default);
}
