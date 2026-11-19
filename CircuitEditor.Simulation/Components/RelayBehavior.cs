using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class RelayBehavior : IComponentBehavior
{
    public IReadOnlyList<string> ComponentTypes => ["relay"];

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component c) =>
        context.Branch(c, "катушка", "COIL+", "COIL-", "R", context.Parameter(c, "coilOhms"));

    public bool Update(BehaviorContext context, Component c)
    {
        var energized = c.Inputs.Values.GetValueOrDefault(ComponentInput.RelayEnergized) > 0;
        var voltage = Math.Abs(context.Voltage(c, "COIL+", "COIL-") ?? 0);
        var next = voltage >= context.Parameter(c, energized ? "releaseVolts" : "pickupVolts");
        return energized != next && c.TrySetInput(ComponentInput.RelayEnergized, next ? 1 : 0);
    }

    public ComponentSignal Observe(BehaviorContext context, Component c) =>
        new(Active: c.Inputs.Values.GetValueOrDefault(ComponentInput.RelayEnergized) > 0);
}
