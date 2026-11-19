using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Engine;

public sealed class SimulationEngine(ISimulationClock clock, int eventLimit = SimulationEngine.EventsPerStep) : ISimulationEngine
{
    public const long StepMicroseconds = 1_000;
    public const int EventsPerStep = 10_000;
    private readonly SimulationEventQueue queue = new();
    private readonly int limit = eventLimit > 0 ? eventLimit : throw new ArgumentOutOfRangeException(nameof(eventLimit));
    public SimulationState State { get; private set; } = new();

    public void Schedule(long microseconds, Action action)
    {
        if (microseconds < clock.Microseconds) throw new ArgumentOutOfRangeException(nameof(microseconds));
        queue.Add(microseconds, action);
    }

    public SimulationState Step(CancellationToken cancellationToken = default)
    {
        if (State.IsHalted) return State;
        var target = checked(clock.Microseconds + StepMicroseconds);
        var count = 0;
        while (queue.TryPeek(out var due) && due <= target)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (count++ >= limit)
                return State = new(clock.Microseconds, true, "Колебательный режим или зацикливание: превышен лимит событий.");
            clock.AdvanceTo(due);
            queue.Take()();
        }

        cancellationToken.ThrowIfCancellationRequested();
        clock.AdvanceTo(target);
        return State = new(target);
    }

    public void Reset()
    {
        queue.Clear();
        clock.Reset();
        State = new();
    }
}
