using System.Collections.Immutable;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class RgbLedBehavior : IComponentBehavior
{
    public string ComponentType => "rgb-led";

    public ComponentSignal Observe(BehaviorContext context, Component component)
    {
        var branches = context.Currents(component).ToArray();
        var channels = new[] { ".R", ".G", ".B" }.Select(channel => LedBehavior.Brightness(
            branches.FirstOrDefault(b => b.Name.EndsWith(channel, StringComparison.Ordinal)))).ToImmutableArray();
        return new(channels.Max(), Channels: channels);
    }
}
