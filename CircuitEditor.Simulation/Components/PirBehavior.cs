using System.Collections.Immutable;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class PirBehavior : IComponentBehavior
{
    public string ComponentType => "pir";

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component c)
    {
        if (!context.For(c).Powered) return [];
        return context.Branch(c, "OUT", "OUT", "GND", "TH", context.Parameter(c, "outputOhms"),
            c.Inputs.Values.GetValueOrDefault(ComponentInput.Motion) > 0 ? context.Parameter(c, "outputVolts") : 0);
    }

    public bool Update(BehaviorContext context, Component c)
    {
        var memory = context.For(c);
        var powered = context.Powered(c);
        if (memory.Powered == powered) return false;
        memory.Powered = powered;
        return true;
    }

    public ComponentSignal Observe(BehaviorContext context, Component c) => new(
        Measurements: context.Powered(c) ? ImmutableDictionary<string,double>.Empty.Add("Motion",
            c.Inputs.Values.GetValueOrDefault(ComponentInput.Motion)) : null);
}
