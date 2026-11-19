namespace CircuitEditor.Simulation.Timing;

internal sealed class SimulationEventQueue
{
    private readonly PriorityQueue<Action, (long Time, long Sequence)> queue = new();
    private long sequence;

    public void Add(long time, Action action) => queue.Enqueue(action, (time, checked(sequence++)));

    public bool TryPeek(out long time)
    {
        var found = queue.TryPeek(out _, out var priority);
        time = priority.Time;
        return found;
    }

    public Action Take() => queue.Dequeue();

    public void Clear()
    {
        queue.Clear();
        sequence = 0;
    }
}
