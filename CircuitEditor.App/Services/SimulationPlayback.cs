using System.Diagnostics;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.App.Services;

public sealed class SimulationPlayback(ISimulationSessionFactory factory) : ISimulationPlayback, IDisposable
{
    private const int FrameMilliseconds = 33;
    private const int SlowFrameMilliseconds = 100;
    private const int WorkerBatch = 32;
    private const int PausedPollingMilliseconds = 8;
    private double speed = 1;
    private SimulationPlaybackRun? current;

    public bool HasSession => current is not null;
    public bool IsRunning => current?.Running == true;
    public double Speed
    {
        get => Volatile.Read(ref speed);
        set { if (double.IsFinite(value)) Volatile.Write(ref speed, Math.Clamp(value, .1, 10)); }
    }

    public SimulationState? TakeFrame() => current?.TakeFrame();

    public void Start(CircuitProject project, ScenarioConfiguration? scenario, bool run) =>
        StartRun(project, run, (copy, _) => factory.Create(copy, scenario));

    public void StartSketch(CircuitProject project, SketchDocument sketch) =>
        StartRun(project, true, (copy, token) => factory.CreateForSketch(copy, sketch, token));

    private void StartRun(CircuitProject project, bool run, Func<CircuitProject, CancellationToken, SimulationSession> create)
    {
        if (HasSession) return;
        var snapshot = new ProjectSnapshot(project);
        var playback = new SimulationPlaybackRun(run);
        current = playback;
        var token = playback.Token;
        _ = Task.Run(async () =>
        {
            SimulationSession? session = null;
            try
            {
                token.ThrowIfCancellationRequested();
                session = create(snapshot.Restore(), token);
                token.ThrowIfCancellationRequested();
                playback.Publish(session.State);
                if (session.HasHalted) return;
                await Play(session, playback, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or ArithmeticException)
            {
                var time = session?.ModelTime ?? 0;
                var message = $"Не удалось продолжить симуляцию. {error.Message}";
                playback.Publish(new(time, true, message) { Diagnostics = [new("SIM_RUNTIME", message, time, IsError: true)] });
            }
        });
    }

    public void Resume() { if (current is { } run) run.Running = true; }
    public void Pause() { if (current is { } run) run.Running = false; }
    public void Step() => current?.RequestStep();
    public void Send(Action<SimulationSession> action) => current?.Commands.Enqueue(action);
    public void SetLatest(object key, Action<SimulationSession> action)
    {
        if (current is { } run) run.Latest[key] = action;
    }

    public void Stop() => Interlocked.Exchange(ref current, null)?.Cancel();

    private async Task Play(SimulationSession session, SimulationPlaybackRun playback, CancellationToken token)
    {
        var wall = Stopwatch.StartNew();
        var previous = wall.ElapsedTicks;
        long lastFrame = 0;
        var available = 0d;
        var dirty = true;
        while (!token.IsCancellationRequested)
        {
            var now = wall.ElapsedTicks;
            var elapsed = (now - previous) * 1_000_000d / Stopwatch.Frequency;
            previous = now;
            var running = playback.Running;
            var rate = Speed;
            available = running ? available + elapsed * rate : 0;
            var steps = playback.TakeSteps();
            if (!session.HasHalted)
            {
                for (var i = 0; i < WorkerBatch && playback.Commands.TryDequeue(out var action); i++)
                {
                    action(session);
                    dirty = true;
                }
                foreach (var key in playback.Latest.Keys)
                    if (playback.Latest.TryRemove(key, out var action)) { action(session); dirty = true; }
                for (var i = 0; i < WorkerBatch &&
                    (steps > 0 || playback.Running && available >= SimulationEngine.StepMicroseconds); i++)
                {
                    session.Advance(token);
                    if (steps > 0) steps--;
                    else available -= SimulationEngine.StepMicroseconds;
                    dirty = true;
                    if (session.HasHalted) break;
                }
                if (steps > 0 && !session.HasHalted) playback.DeferSteps(steps);
            }

            var cadence = available > SlowFrameMilliseconds * 1000 * rate ? SlowFrameMilliseconds : FrameMilliseconds;
            if (dirty && (wall.ElapsedMilliseconds - lastFrame >= cadence || session.HasHalted))
            {
                var next = session.Observe();
                playback.Publish(next);
                if (next.IsHalted) return;
                dirty = false;
                lastFrame = wall.ElapsedMilliseconds;
            }
            if (playback.Running && available >= SimulationEngine.StepMicroseconds) await Task.Yield();
            else await Task.Delay(running ? 1 : PausedPollingMilliseconds, token).ConfigureAwait(false);
        }
    }

    public void Dispose() => Stop();
}
