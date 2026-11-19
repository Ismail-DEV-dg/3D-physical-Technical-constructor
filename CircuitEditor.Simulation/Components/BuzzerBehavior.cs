using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class BuzzerBehavior : IComponentBehavior
{
    public string ComponentType => "buzzer";

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component c) =>
        context.Branch(c, "пьезо", "+", "-", "R", context.Parameter(c, "resistanceOhms"));

    public ComponentSignal Observe(BehaviorContext context, Component c) =>
        new(Active: context.Powered(c, "+", "-"));
}
