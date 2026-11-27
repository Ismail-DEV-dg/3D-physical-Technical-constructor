using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneHover
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);
    private readonly FrameworkElement target;
    private readonly ToolTip tip;
    private readonly DispatcherTimer timer;
    private readonly Func<Point, SceneNode?> find;
    private Point? pending;

    public SceneHover(FrameworkElement target, Func<Point, SceneNode?> find)
    {
        this.target = target;
        this.find = find;
        tip = new ToolTip { PlacementTarget = target, Placement = PlacementMode.Mouse, IsHitTestVisible = false };
        timer = new DispatcherTimer(DispatcherPriority.Background, target.Dispatcher) { Interval = Delay };
        timer.Tick += OnDelay;
    }

    public void Observe(Point? point)
    {
        if (pending == point) return;
        pending = point;
        timer.Stop();
        tip.IsOpen = false;
        if (point.HasValue) timer.Start();
    }

    private void OnDelay(object? sender, EventArgs args)
    {
        timer.Stop();
        if (pending is not { } point || !target.IsMouseOver || find(point) is not { } node) return;
        tip.Content = string.Join(" · ", new[] { node.Label, node.Name, node.Value, node.Color }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        tip.IsOpen = true;
    }
}
