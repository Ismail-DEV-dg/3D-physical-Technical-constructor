using System.Collections.Concurrent;
using System.Diagnostics;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.App.Services;

public sealed class SimulationPlayback(ISimulationSessionFactory factory) : ISimulationPlayback, IDisposable
{
    private const int FrameMilliseconds = 33;
    private const int WorkerBatch = 32;
    private readonly object deliveryGate = new();
    private double speed = 1;
    private ConcurrentQueue<Action<SimulationSession>> edits = new();
    private ConcurrentDictionary<object, Action<SimulationSession>> latest = new();
    private bool running;
    private int pendingSteps;
    private CancellationTokenSource? lifetime;
    private SimulationState? frame;
    private int generation;

    public bool HasSession => lifetime is not null;
    public bool IsRunning => Volatile.Read(ref running);
    public double Speed { get => Volatile.Read(ref speed); set => Volatile.Write(ref speed, Math.Clamp(value, .1, 10)); }

    public SimulationState? TakeFrame() => Interlocked.Exchange(ref frame, null);

    public void Start(CircuitProject project, ScenarioConfiguration? scenario, bool run)
    {
        if (HasSession) return;
        var frozen = new ProjectSnapshot(project).Restore();
        Volatile.Write(ref running, run);
        Volatile.Write(ref pendingSteps, run ? 0 : 1);
        lifetime = new();
        var token = lifetime.Token;
        var id = Interlocked.Increment(ref generation);
        var commands = edits;
        var changes = latest;
        _ = Task.Run(async () =>
        {
            try
            {
                await Play(factory.Create(frozen, scenario), id, commands, changes, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or ArithmeticException)
            {
                var message = $"Не удалось продолжить симуляцию. {error.Message}";
                Publish(id, new(0, true, message) { Diagnostics = [new("SIM_RUNTIME", message, 0, IsError: true)] });
            }
        });
    }

    public void Resume() => Volatile.Write(ref running, true);
    public void Pause() => Volatile.Write(ref running, false);
    public void Step() => Interlocked.Increment(ref pendingSteps);
    public void Send(Action<SimulationSession> action) { if (HasSession) edits.Enqueue(action); }
    public void SetLatest(object key, Action<SimulationSession> action) { if (HasSession) latest[key] = action; }

    public void Stop()
    {
        Interlocked.Increment(ref generation);
        lifetime?.Cancel();
        lifetime?.Dispose();
        lifetime = null;
        Pause();
        frame = null;
        edits = new();
        latest = new();
    }

    private async Task Play(SimulationSession session, int id, ConcurrentQueue<Action<SimulationSession>> commands,
        ConcurrentDictionary<object, Action<SimulationSession>> changes, CancellationToken token)
    {
        var wall = Stopwatch.StartNew();
        var previous = wall.ElapsedTicks;
        long lastFrame = -FrameMilliseconds;
        var available = 0d;
        var dirty = true;
        while (!token.IsCancellationRequested)
        {
            var now = wall.ElapsedTicks;
            var elapsed = (now - previous) * 1_000_000d / Stopwatch.Frequency;
            previous = now;
            if (id != Volatile.Read(ref generation)) return;
            available = IsRunning ? available + elapsed * Speed : 0;
            var steps = Interlocked.Exchange(ref pendingSteps, 0);
            for (var i = 0; i < WorkerBatch && commands.TryDequeue(out var action); i++)
            {
                action(session);
                dirty = true;
            }
            foreach (var key in changes.Keys)
                if (changes.TryRemove(key, out var action)) { action(session); dirty = true; }
            if (!session.State.IsHalted)
            {
                for (var i = 0; i < WorkerBatch && (steps > 0 || available >= SimulationEngine.StepMicroseconds); i++)
                {
                    session.Advance(token);
                    if (steps > 0) steps--;
                    else available -= SimulationEngine.StepMicroseconds;
                    dirty = true;
                    if (session.HasHalted) break;
                }
                if (steps > 0 && !session.HasHalted) Interlocked.Add(ref pendingSteps, steps);
            }
            if (dirty && (wall.ElapsedMilliseconds - lastFrame >= FrameMilliseconds || session.HasHalted))
            {
                var next = session.Observe();
                if (id != Volatile.Read(ref generation)) return;
                if (next.IsHalted) Pause();
                Interlocked.Exchange(ref frame, next);
                dirty = false;
                lastFrame = wall.ElapsedMilliseconds;
            }
            if (available >= SimulationEngine.StepMicroseconds && !session.HasHalted) await Task.Yield();
            else await Task.Delay(1, token).ConfigureAwait(false);
        }
    }

    private void Publish(int id, SimulationState value)
    {
        lock (deliveryGate)
        {
            if (id != generation) return;
            if (value.IsHalted) Pause();
            Interlocked.Exchange(ref frame, value);
        }
    }

    public void Dispose() => Stop();
}
