using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CircuitEditor.App.Interaction;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class Stage10Benchmark(IComponentLibraryRepository library, IFootprintRepository footprints,
    IBreadboardRepository boards, IProjectRepository documents, IExportRepository export, ProjectSessionViewModel session,
    ProjectViewModel project, BreadboardViewModel mockup, ThreeDViewModel threeD, SimulationViewModel simulation) : IStage10Benchmark
{
    public Window CreateWindow(string report)
    {
        ContentControl content = new();
        Window window = new() { Width = 1200, Height = 800, Title = "Circuit Editor · проверка производительности", Content = content };
        window.Loaded += (_, _) => Run(content, report);
        return window;
    }

    private async void Run(ContentControl content, string report)
    {
        try
        {
            var path = Path.ChangeExtension(report, ".circ");
            var design = SceneBenchmarkDesign.Create(library, footprints, boards);
            foreach (var part in design.Circuit.Components)
                design.SchematicLayout.Set(part.Id, design.MockupLayout.Get(part.Id));
            var view = new ProjectViewState(MockupZoom: .78, MockupX: 255, MockupY: 30);
            if (documents.Save(path, ProjectDocument.Capture(design, view)) is { } failure)
                throw new InvalidOperationException(failure);
            var dpi = VisualTreeHelper.GetDpi(content);
            var clock = Stopwatch.StartNew();
            Open(path);
            await ShowMockup(content);
            var opened = clock.Elapsed.TotalMilliseconds;
            var drag = await RenderFor(3, DragInput(content));
            mockup.InputCommand.Execute(new(CanvasAction.Cancel));

            await StartSimulation();
            var animation = await RenderFor(3);
            simulation.ResetCommand.Execute(null);

            await Idle();
            var before = Memory();
            for (var i = 0; i < 20; i++)
            {
                project.ReplaceProject(new CircuitProject(footprints: design.Footprints));
                Open(path);
                await Idle();
            }
            var opened20 = Memory();
            for (var i = 0; i < 50; i++)
            {
                await StartSimulation();
                simulation.ResetCommand.Execute(null);
            }
            await Idle();
            var simulated50 = Memory();

            List<WeakReference> views = [];
            for (var i = 0; i < 50; i++)
            {
                var item = new ThreeDView { DataContext = threeD };
                views.Add(new(item));
                content.Content = item;
                await Idle();
                await ShowMockup(content);
            }
            await Idle();
            var switched50 = Memory();
            Write(report, new { design.Circuit.Components.Count, Wires = design.Circuit.Wires.Count,
                Width = content.ActualWidth, Height = content.ActualHeight, Dpi = dpi.PixelsPerInchX, OpenMs = opened,
                Drag = new { drag.Fps, drag.P95 }, SimulationFps = animation.Fps, before, opened20, simulated50,
                switched50, RetainedViews = views.Count(v => v.IsAlive) });
            Application.Current.Shutdown(0);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Write(report, new { Error = error.Message });
            Application.Current.Shutdown(1);
        }
    }

    private Action<double> DragInput(ContentControl content)
    {
        var uno = project.Components.Single(c => c.Model.Definition.Id == "arduino-uno");
        var placement = mockup.PlacementOf(uno);
        var scale = mockup.Zoom * PhysicalSettings.PixelsPerMillimeter;
        var x = mockup.OffsetX + (placement.X + 15) * scale;
        var y = mockup.OffsetY + (placement.Y + 18) * scale;
        mockup.InputCommand.Execute(new(CanvasAction.Down, x, y));
        return seconds =>
        {
            mockup.InputCommand.Execute(new(CanvasAction.Move, x + Math.Sin(seconds * 4) * 18, y));
            content.UpdateLayout();
        };
    }

    private void Open(string path)
    {
        if (!session.Load(path)) throw new InvalidOperationException("Не удалось открыть стенд.");
    }

    private async Task ShowMockup(ContentControl content)
    {
        content.Content = new BreadboardView { DataContext = mockup };
        await Idle();
    }

    private async Task StartSimulation()
    {
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void Seen(object? sender, Core.Simulation.SimulationState state)
        {
            if (state.IsHalted) ready.TrySetException(new InvalidOperationException(state.Failure));
            else if (state.Microseconds > 0) ready.TrySetResult();
        }
        simulation.FrameChanged += Seen;
        try
        {
            simulation.StartCircuitCommand.Execute(null);
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally { simulation.FrameChanged -= Seen; }
    }

    private static async Task<(double Fps, double P95)> RenderFor(double seconds, Action<double>? input = null)
    {
        List<double> delays = [];
        List<double> frames = [];
        var time = Stopwatch.StartNew();
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TimeSpan prior = TimeSpan.MinValue;
        void Frame(object? sender, EventArgs args)
        {
            if (args is not RenderingEventArgs render || render.RenderingTime == prior) return;
            if (prior != TimeSpan.MinValue) frames.Add((render.RenderingTime - prior).TotalMilliseconds);
            prior = render.RenderingTime;
            if (input is not null)
            {
                var start = Stopwatch.GetTimestamp();
                input(time.Elapsed.TotalSeconds);
                delays.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
            if (time.Elapsed.TotalSeconds >= seconds) ready.TrySetResult();
        }
        CompositionTarget.Rendering += Frame;
        try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(15)); }
        finally { CompositionTarget.Rendering -= Frame; }
        delays.Sort();
        return (1000 / frames.Average(), delays.Count == 0 ? 0 : delays[(int)Math.Ceiling(delays.Count * .95) - 1]);
    }

    private static async Task Idle() => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

    private static object Memory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        using var process = Process.GetCurrentProcess();
        return new { ManagedBytes = GC.GetTotalMemory(false), process.PrivateMemorySize64 };
    }

    private void Write(string report, object values) => export.Write(report,
        JsonSerializer.SerializeToUtf8Bytes(values, new JsonSerializerOptions { WriteIndented = true }));
}
