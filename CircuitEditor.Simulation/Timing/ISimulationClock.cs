namespace CircuitEditor.Simulation.Timing;

public interface ISimulationClock
{
    long Microseconds { get; }
    void AdvanceTo(long microseconds);
    void Reset();
}
