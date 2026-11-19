using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Simulation.Components;

public sealed class DiodeBehavior : IComponentBehavior
{
    public IReadOnlyList<string> ComponentTypes => ["diode"];

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component c) =>
        context.Branch(c, "диод", "A", "K", "LED",
            context.Parameter(c, "dynamicOhms"), context.Parameter(c, "forwardVolts"));
}
