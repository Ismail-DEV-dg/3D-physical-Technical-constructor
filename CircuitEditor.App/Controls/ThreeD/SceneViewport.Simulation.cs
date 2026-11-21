using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Simulation;
using HelixToolkit.Wpf;

namespace CircuitEditor.App.Controls.ThreeD;

public sealed partial class SceneViewport
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromSeconds(1d / 30);
    private SceneReactionBuilder reactions = new();
    private CancellationTokenSource? animation;
    private SimulationState? latestFrame;
    private long lastFrame;
    private SceneNode? interacting;

    private void QueueFrame(object? sender, SimulationState state)
    {
        latestFrame = state;
        if (animation is null) Animate();
    }

    private async void Animate()
    {
        if (workspace is not { } vm || latestFrame is null || nodes.Count == 0) return;
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
                var source = nodes.Values.Select(v => v.Source).ToArray();
                var builder = reactions;
                var voltage = vm.Simulation?.ShowVoltage == true;
                var frame = await Task.Run(() => builder.Build(source, state, voltage, run.Token), run.Token);
                if (animation != run) return;
                foreach (var value in frame)
                    if (nodes.TryGetValue(value.Node, out var visual))
                        visual.React(value, vm.SelectedId == value.Node);
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

    private void SimulationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ViewModels.SimulationViewModel.ShowVoltage) && workspace?.Simulation is { } sim)
            QueueFrame(sender, sim.State);
    }

    private void ClearFrames()
    {
        animation?.Cancel();
        animation = null;
        latestFrame = null;
        reactions = new();
    }

    private bool BeginSimulationInteraction(Point point)
    {
        if (workspace?.Simulation is not { HasSession: true } sim || Find(point) is not { } node) return false;
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
        CaptureMouse();
        return true;
    }

    private void MoveKnob(Point point)
    {
        if (interacting is not { Knob: { } knob } node || workspace?.Simulation is not { } sim ||
            KnobPoint(node, point) is not { } at) return;
        var previous = sim.State.Components.GetValueOrDefault(node.Id)?.Angle ?? node.Angle;
        sim.Turn(node.Id, CircuitEditor.Core.Physical.PotentiometerPosition.FromPointer(at.X - knob.X, at.Y - knob.Y, previous));
    }

    private ScenePoint? KnobPoint(SceneNode node, Point point)
    {
        if (Viewport is null) return null;
        var height = node.Pose.Origin.Z + node.Profile.Clearance + node.Profile.Height + node.Profile.DetailHeight;
        return Viewport3DHelper.UnProject(Viewport, point, new(0, 0, height), new(0, 0, 1)) is { } at
            ? node.Pose.Local(new(at.X, -at.Y, at.Z)) : null;
    }

    private void ReleaseInteraction()
    {
        var part = interacting;
        interacting = null;
        if (part?.Family == "button") workspace?.Simulation?.Press(part.Id, false);
        if (part is not null && IsMouseCaptured) ReleaseMouseCapture();
    }

    protected override void OnLostMouseCapture(MouseEventArgs args)
    {
        ReleaseInteraction();
        base.OnLostMouseCapture(args);
    }
}
