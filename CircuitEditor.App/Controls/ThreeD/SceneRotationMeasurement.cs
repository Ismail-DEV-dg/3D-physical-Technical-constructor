using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneRotationMeasurement
{
    private const double AngularSpeed = .8;
    private static readonly TimeSpan Warmup = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Sample = TimeSpan.FromSeconds(5);

    public static async Task<double> Measure(SceneViewport viewport, CancellationToken token)
    {
        if (viewport.Camera is not ProjectionCamera camera)
            throw new InvalidOperationException("Камера вьюпорта недоступна.");

        var initial = camera.Position;
        var target = initial + camera.LookDirection;
        var radius = initial - target;
        var stopwatch = Stopwatch.StartNew();
        var completion = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
        var last = TimeSpan.MinValue;
        var count = 0;
        var first = TimeSpan.Zero;
        using var registration = token.Register(() => completion.TrySetCanceled(token));
        CompositionTarget.Rendering += Frame;
        try { return await completion.Task.WaitAsync(Warmup + Sample + TimeSpan.FromSeconds(15), token); }
        finally { CompositionTarget.Rendering -= Frame; }

        void Frame(object? sender, EventArgs args)
        {
            if (args is not RenderingEventArgs rendered || rendered.RenderingTime == last) return;
            last = rendered.RenderingTime;
            var seconds = stopwatch.Elapsed.TotalSeconds;
            var angle = seconds * AngularSpeed;
            var v = new Vector3D(radius.X * Math.Cos(angle) - radius.Y * Math.Sin(angle),
                radius.X * Math.Sin(angle) + radius.Y * Math.Cos(angle), radius.Z);
            camera.Position = target + v;
            camera.LookDirection = -v;
            if (stopwatch.Elapsed < Warmup) return;
            if (count++ == 0) first = last;
            var elapsed = last - first;
            if (elapsed >= Sample) completion.TrySetResult((count - 1) / elapsed.TotalSeconds);
        }
    }
}
