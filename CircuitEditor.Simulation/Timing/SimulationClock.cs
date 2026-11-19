namespace CircuitEditor.Simulation.Timing;

public sealed class SimulationClock : ISimulationClock
{
    public long Microseconds { get; private set; }

    public void AdvanceTo(long microseconds)
    {
        if (microseconds < Microseconds) throw new ArgumentOutOfRangeException(nameof(microseconds));
        Microseconds = microseconds;
    }

    public void Reset() => Microseconds = 0;
}
