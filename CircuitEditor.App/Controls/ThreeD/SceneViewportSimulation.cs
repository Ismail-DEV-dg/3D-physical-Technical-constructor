using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Simulation;
using HelixToolkit.Wpf;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneViewportSimulation(SceneViewport owner)
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromSeconds(1d / 30);
    private SceneReactionBuilder reactions = new();
    private CancellationTokenSource? animation;
    private SimulationState? latestFrame;
    private long lastFrame;
    private SceneNode? interacting;

    public bool IsInteracting => interacting is not null;

    public void QueueFrame(object? sender, SimulationState state)
    {
        latestFrame = state;
        if (animation is null) Animate();
    }

    private async void Animate()
    {
        if (owner.Workspace is not { } vm || latestFrame is null || owner.SceneNodes.Count == 0) return;
        using CancellationTokenSource run = new();
        animation = run;
        try
        {
            while (latestFrame is not null && animation == run)
            {
                var delay = FrameInterval - Stopwatch.GetElapsedTime(lastFrame);
                if (delay > TimeSpan.Zero) await Task.Delay(delay, run.Token);
                if (animation != run) return;
                var state = latestFrame;
                latestFrame = null;
                if (state is null) return;
                var source = owner.SceneNodes.Values.Select(v => v.Source).ToArray();
                var builder = reactions;
                var voltage = vm.Simulation?.ShowVoltage == true;
                var frame = await Task.Run(() => builder.Build(source, state, voltage, run.Token), run.Token);
                if (animation != run) return;
                var selection = vm.SelectedId;
                foreach (var value in frame)
                    if (owner.SceneNodes.TryGetValue(value.Node, out var visual))
                        visual.React(value, selection == value.Node);
                lastFrame = Stopwatch.GetTimestamp();
            }
        }
        catch (OperationCanceledException) when (run.IsCancellationRequested) { }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (animation == run) vm.Notice = $"Не удалось показать состояние симуляции. {error.Message}";
        }
        finally { if (animation == run) animation = null; }
    }

    public void SimulationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SimulationViewModel.ShowVoltage) && owner.Workspace?.Simulation is { } sim)
            QueueFrame(sender, sim.State);
    }

    public void ClearFrames()
    {
        animation?.Cancel();
        animation = null;
        latestFrame = null;
        reactions = new();
    }

    public bool BeginInteraction(Point point)
    {
        if (owner.Workspace?.Simulation is not { HasSession: true } sim || owner.Find(point) is not { } node) return false;
        if (node.Family == "button")
        {
            interacting = node;
            sim.Press(node.Id, true);
        }
        else if (node.Knob is { } knob && KnobPoint(node, point) is { } local &&
            (local.X - knob.X) * (local.X - knob.X) + (local.Y - knob.Y) * (local.Y - knob.Y) <=
            node.KnobRadius * node.KnobRadius)
        {
            interacting = node;
            MoveKnob(point);
        }
        if (interacting is null) return false;
        owner.CaptureMouse();
        return true;
    }

    public void MoveKnob(Point point)
    {
        if (interacting is not { Knob: { } knob } node || owner.Workspace?.Simulation is not { } sim ||
            KnobPoint(node, point) is not { } at) return;
        var previous = sim.State.Components.GetValueOrDefault(node.Id)?.Angle ?? node.Angle;
        sim.Turn(node.Id, CircuitEditor.Core.Physical.PotentiometerPosition.FromPointer(at.X - knob.X, at.Y - knob.Y, previous));
    }

    private ScenePoint? KnobPoint(SceneNode node, Point point)
    {
        if (owner.Viewport is not { } viewport) return null;
        var height = node.Pose.Origin.Z + node.Profile.Clearance + node.Profile.Height + node.Profile.DetailHeight;
        return Viewport3DHelper.UnProject(viewport, point, new(0, 0, height), new(0, 0, 1)) is { } at
            ? node.Pose.Local(new(at.X, -at.Y, at.Z)) : null;
    }

    public void ReleaseInteraction()
    {
        var part = interacting;
        interacting = null;
        if (part?.Family == "button") owner.Workspace?.Simulation?.Press(part.Id, false);
        if (part is not null && owner.IsMouseCaptured) owner.ReleaseMouseCapture();
    }
}
