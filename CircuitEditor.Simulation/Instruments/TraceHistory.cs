using System.Collections.Immutable;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Instruments;

public sealed class TraceHistory(int capacity = TraceHistory.DefaultCapacity)
{
    public const int DefaultCapacity = 4096;
    private readonly TraceSample?[] entries = new TraceSample[capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity))];
    private int oldest;
    private int count;

    public void Add(TraceSample value)
    {
        if (count == entries.Length)
        {
            entries[oldest] = value;
            oldest = (oldest + 1) % entries.Length;
        }
        else
        {
            entries[(oldest + count) % entries.Length] = value;
            count++;
        }
    }

    public ImmutableArray<TraceSample> Snapshot()
    {
        var result = ImmutableArray.CreateBuilder<TraceSample>(count);
        for (var i = 0; i < count; i++) result.Add(entries[(oldest + i) % entries.Length]!);
        return result.MoveToImmutable();
    }

    public void Clear()
    {
        oldest = count = 0;
        Array.Clear(entries);
    }
}
