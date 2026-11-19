using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using System.Collections.Immutable;

namespace CircuitEditor.Simulation.Components;

public sealed class LedBehavior : IComponentBehavior
{
    public IReadOnlyList<string> ComponentTypes => ["led", "rgb-led"];

    public ComponentSignal Observe(BehaviorContext context, Component component)
    {
        var levels = context.Currents(component)
            .Select(b => (b.Name, Level: Math.Clamp((b.Current ?? 0) / 0.02, 0, 1))).ToArray();
        if (component.Definition.Id == "led") return new(Intensity: levels.FirstOrDefault().Level);
        var channels = new[] { "R", "G", "B" }.Select(channel => levels.FirstOrDefault(l =>
            l.Name.EndsWith("." + channel, StringComparison.Ordinal)).Level).ToImmutableArray();
        return new(channels.Max(), Channels: channels);
    }
}
