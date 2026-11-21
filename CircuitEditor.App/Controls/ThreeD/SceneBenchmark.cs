using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneBenchmark(IComponentLibraryRepository components, IFootprintRepository footprints,
    IBreadboardRepository breadboards, IExportRepository export, ProjectViewModel project, SimulationViewModel simulation,
    ThreeDViewModel viewModel) : ISceneBenchmark
{
    public Window CreateWindow(string reportPath)
    {
        var stage = new Grid();
        var window = new Window { Title = "Circuit Editor · измерение 3D", Width = 1200, Height = 800, Content = stage };
        window.SetResourceReference(Window.BackgroundProperty, "CanvasFieldBrush");
        CancellationTokenSource lifetime = new();
        window.Closed += (_, _) => lifetime.Cancel();
        window.Loaded += (_, _) => Run(window, stage, reportPath, lifetime.Token);
        return window;
    }

    private async void Run(Window window, Grid stage, string path, CancellationToken token)
    {
        try
        {
            var results = new List<SceneBenchmarkResult>
            {
                await Sample(stage, false, token), await Sample(stage, true, token)
            };
            var error = export.Write(path, JsonSerializer.SerializeToUtf8Bytes(results, new JsonSerializerOptions { WriteIndented = true }));
            if (error is not null) throw new InvalidOperationException(error);
            Application.Current.Shutdown(0);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            export.Write(path, JsonSerializer.SerializeToUtf8Bytes(new { Error = error.ToString() }));
            Application.Current.Shutdown(1);
        }
    }

    private async Task<SceneBenchmarkResult> Sample(Grid stage, bool runSimulation, CancellationToken token)
    {
        project.ReplaceProject(SceneBenchmarkDesign.Create(components, footprints, breadboards));
        var viewport = new SceneViewport { DataContext = viewModel };
        var initial = await MeasureUpdate(viewport, () => stage.Children.Add(viewport), token);
        var change = await MeasureUpdate(viewport, () =>
        {
            var node = project.Model.Circuit.Components.Last(c => c.Definition.Id == "resistor");
            if (!node.TryUpdateProperties("330 Ω", null, null, out var error))
                throw new InvalidOperationException(error);
            project.Refresh();
        }, token);

        var litFrames = 0;
        var peakVoltageWires = 0;
        void Observe(object? sender, Core.Simulation.SimulationState frame)
        {
            if (frame.Components.Values.Count(c => c.Intensity > 0) > 1) litFrames++;
            peakVoltageWires = Math.Max(peakVoltageWires, frame.Wires.Values.Count(w => w.Voltage.HasValue));
        }
        simulation.FrameChanged += Observe;
        double fps;
        try
        {
            if (runSimulation) simulation.StartCommand.Execute(null);
            fps = await SceneRotationMeasurement.Measure(viewport, token);
        }
        finally { simulation.FrameChanged -= Observe; }
        var modelTime = simulation.State.Microseconds;
        if (runSimulation && modelTime <= 0)
            throw new InvalidOperationException(simulation.State.Failure ?? "Старт симуляции не подтверждён.");
        if (runSimulation) simulation.ResetCommand.Execute(null);
        var result = new SceneBenchmarkResult(runSimulation, false, project.Model.Circuit.Components.Count,
            project.Model.Circuit.Wires.Count, viewport.Viewport.ActualWidth, viewport.Viewport.ActualHeight,
            RenderCapability.Tier >> 16, initial, change, fps, modelTime, litFrames, peakVoltageWires);
        stage.Children.Remove(viewport);
        return result;
    }

    private static async Task<double> MeasureUpdate(SceneViewport viewport, Action trigger, CancellationToken token)
    {
        TaskCompletionSource<double> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var clock = Stopwatch.StartNew();
        void Applied(object? sender, EventArgs args) => ready.TrySetResult(clock.Elapsed.TotalMilliseconds);
        viewport.SceneApplied += Applied;
        try
        {
            trigger();
            return await ready.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
        }
        catch (TimeoutException)
        {
            var message = (viewport.DataContext as ThreeDViewModel)?.Notice;
            throw new TimeoutException("Не получено обновление 3D-сцены. " + message);
        }
        finally { viewport.SceneApplied -= Applied; }
    }
}
