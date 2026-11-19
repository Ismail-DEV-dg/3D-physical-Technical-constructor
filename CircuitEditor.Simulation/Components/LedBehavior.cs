using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class LedBehavior : IComponentBehavior
{
    public string ComponentType => "led";

    public ComponentSignal Observe(BehaviorContext context, Component component) =>
        new(Intensity: Brightness(context.Currents(component).FirstOrDefault()));

    internal static double Brightness(DcBranchResult? branch) =>
        branch is { Limit: > 0 } ? Math.Clamp((branch.Current ?? 0) / branch.Limit, 0, 1) : 0;
}
