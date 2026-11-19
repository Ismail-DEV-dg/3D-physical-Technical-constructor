using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Engine;

public interface ISimulationEngine
{
    SimulationState State { get; }
    void Schedule(long microseconds, Action action);
    SimulationState Step(CancellationToken cancellationToken = default);
    void Reset();
}
