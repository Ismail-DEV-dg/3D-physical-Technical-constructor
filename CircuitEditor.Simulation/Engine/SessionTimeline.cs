using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Engine;

internal sealed class SessionTimeline(SimulationSession session) : ISimulationEngine
{
    public SimulationState State => session.State;
    public void Schedule(long microseconds, Action action) => session.Schedule(microseconds, action);
    public SimulationState Step(CancellationToken cancellationToken = default) => session.Step(cancellationToken);
    public void Reset() => throw new NotSupportedException("Создайте новый экземпляр сеанса.");
}
