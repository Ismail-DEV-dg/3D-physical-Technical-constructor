using CircuitEditor.App.Interaction;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Scenarios;
using CircuitEditor.Simulation.Sketches;

namespace CircuitEditor.Tests;

public sealed class SketchLaunchTests
{
    [Fact]
    public void GlobalShortcutOpensMockupAndCircuitStartUsesSeparateSession()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var env = design.Environment;
        var scenarios = new JsonScenarioRepository();
        using var playback = CreatePlayback(scenarios);
        var pulse = new TestSimulationPulse();
        using var sim = new SimulationViewModel(env.Project, playback, pulse, scenarios);
        using var main = new MainViewModel(env.Navigation, new ThemeService(new TestThemeResources()),
            new TestWindowService(), simulation: sim);

        env.Project.UpdateSketch(new("void setup() { pinMode(13, OUTPUT); digitalWrite(13, HIGH); } void loop() {}"));
        main.SelectedWorkspace = main.Code;
        ShortcutCommands.Execute(main, "RunSketch");
        Assert.Same(main.Mockup, main.SelectedWorkspace);
        Assert.True(sim.IsSketchSession);
        Assert.True(Wait(() => sim.State.Components.GetValueOrDefault(uno.Model.Id)?.Intensity == 1));
        Assert.StartsWith("Код", sim.Status);

        sim.StartCircuitCommand.Execute(null);
        Assert.False(sim.IsSketchSession);
        Assert.True(Wait(() => sim.State.Components.GetValueOrDefault(uno.Model.Id)?.Intensity == 0));
        Assert.StartsWith("Схема", sim.Status);

        env.Project.UpdateSketch(new("void setup() {}\nvoid loop() { unknown(); }"));
        ShortcutCommands.Execute(main, "RunSketch");
        Assert.True(Wait(() => sim.State.IsHalted));
        Assert.Same(main.Code, main.SelectedWorkspace);
        Assert.Equal(2, main.Code.RequestedLine);
        Assert.Contains(main.Diagnostics.Items, item => item.Code == "CODE_RUN");
        Assert.False(sim.IsRunning);

        bool Wait(Func<bool> condition) => SpinWait.SpinUntil(() =>
        {
            pulse.TickOnce();
            return condition();
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TogglePausesAndContinuesTheSameSketchWithoutRestarting()
    {
        using var design = new ElectricalTestCircuit();
        design.Add("arduino-uno");
        design.Project.UpdateSketch(new(SketchExecutionTests.Blink));
        var scenarios = new JsonScenarioRepository();
        using var playback = CreatePlayback(scenarios);
        using var sim = new SimulationViewModel(design.Project, playback, new TestSimulationPulse(), scenarios);
        var starts = 0;
        sim.SketchStarted += (_, _) => starts++;

        Assert.Equal("Запустить код", sim.SketchPlaybackAction);
        sim.ToggleSketchCommand.Execute(null);
        Assert.True(sim.IsSketchRunning);
        Assert.Equal("Пауза кода", sim.SketchPlaybackAction);
        var started = await ReadSession();

        sim.ToggleSketchCommand.Execute(null);
        Assert.True(sim.HasSession);
        Assert.False(sim.IsSketchRunning);
        Assert.Equal("Продолжить код", sim.SketchPlaybackAction);
        var paused = await ReadSession();
        await Task.Delay(35);
        var stillPaused = await ReadSession();
        Assert.Same(started.Session, paused.Session);
        Assert.Equal(paused, stillPaused);

        sim.ToggleSketchCommand.Execute(null);
        Assert.True(sim.IsSketchRunning);
        Assert.Equal("Пауза кода", sim.SketchPlaybackAction);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var resumed = await ReadSession();
        while (resumed.Time <= paused.Time)
        {
            await Task.Delay(10, timeout.Token);
            resumed = await ReadSession();
        }
        Assert.Same(started.Session, resumed.Session);
        Assert.Equal(1, starts);

        sim.ResetCommand.Execute(null);
        Assert.False(sim.HasSession);
        Assert.Equal("Запустить код", sim.SketchPlaybackAction);

        async Task<(SimulationSession Session, long Time)> ReadSession()
        {
            TaskCompletionSource<(SimulationSession, long)> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            playback.Send(session => result.TrySetResult((session, session.ModelTime)));
            return await result.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static SimulationPlayback CreatePlayback(JsonScenarioRepository scenarios)
    {
        var electrical = new JsonElectricalLibraryRepository();
        return new(new SimulationSessionFactory(new NetlistBuilder(electrical), new DcSolver(electrical),
            new JsonSimulationModelRepository(), new ScenarioRunner(scenarios), [],
            sketches: new SketchRunner(new JsonCodeTemplateRepository())));
    }
}
