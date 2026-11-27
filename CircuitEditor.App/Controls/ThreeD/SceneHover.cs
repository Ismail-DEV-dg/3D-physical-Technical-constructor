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
    private Guid? id;

    public SceneHover(FrameworkElement target)
    {
        this.target = target;
        tip = new ToolTip { PlacementTarget = target, Placement = PlacementMode.Mouse, IsHitTestVisible = false };
        timer = new DispatcherTimer(DispatcherPriority.Background, target.Dispatcher) { Interval = Delay };
        timer.Tick += OnDelay;
    }

    public void Observe(SceneNode? node)
    {
        if (id == node?.Id) return;
        id = node?.Id;
        timer.Stop();
        tip.IsOpen = false;
        if (node is null) return;
        tip.Content = string.Join(" · ", new[] { node.Label, node.Name, node.Value, node.Color }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        timer.Start();
    }

    private void OnDelay(object? sender, EventArgs args)
    {
        timer.Stop();
        if (id is not null && target.IsMouseOver) tip.IsOpen = true;
    }
}
