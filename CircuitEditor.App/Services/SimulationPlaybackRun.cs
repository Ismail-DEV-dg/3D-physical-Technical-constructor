using System.Collections.Concurrent;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.App.Services;

internal sealed class SimulationPlaybackRun(bool start)
{
    private readonly CancellationTokenSource lifetime = new();
    private int running = start ? 1 : 0;
    private int steps = start ? 0 : 1;
    private SimulationState? frame;

    internal ConcurrentQueue<Action<SimulationSession>> Commands { get; } = new();
    internal ConcurrentDictionary<object, Action<SimulationSession>> Latest { get; } = new();
    internal CancellationToken Token => lifetime.Token;
    internal bool Running
    {
        get => Volatile.Read(ref running) != 0;
        set => Volatile.Write(ref running, value ? 1 : 0);
    }

    internal void RequestStep() => Interlocked.Increment(ref steps);
    internal int TakeSteps() => Interlocked.Exchange(ref steps, 0);
    internal void DeferSteps(int count) => Interlocked.Add(ref steps, count);
    internal SimulationState? TakeFrame() => Interlocked.Exchange(ref frame, null);

    internal void Publish(SimulationState value)
    {
        if (value.IsHalted) Running = false;
        Interlocked.Exchange(ref frame, value);
    }

    internal void Cancel()
    {
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
