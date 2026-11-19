using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Engine;

public interface ISimulationEngine
{
    ISimulationClock Clock { get; }
    SimulationState State { get; }
    void Schedule(long microseconds, Action action);
    SimulationState Step(CancellationToken cancellationToken = default);
    void Halt(string reason);
    void Reset();
}
