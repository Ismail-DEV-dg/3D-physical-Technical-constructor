using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class SevenSegmentBehavior : IComponentBehavior
{
    private static readonly string[] SegmentPins = ["A", "B", "C", "D", "E", "F", "G", "DP"];
    public string ComponentType => "seven-segment";

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component component)
    {
        var resistance = context.Parameter(component, "dynamicOhms");
        var forward = context.Parameter(component, "forwardVolts");
        return SegmentPins.SelectMany(pin => context.Branch(component, pin, pin, "COM", "LED", resistance, forward));
    }

    public ComponentSignal Observe(BehaviorContext context, Component component)
    {
        var currents = context.Currents(component).ToDictionary(branch => branch.PinA ?? "", branch => branch.Current);
        var minimum = context.Parameter(component, "visibleAmps");
        var segments = 0;
        for (var i = 0; i < SegmentPins.Length; i++)
            if (currents.GetValueOrDefault(SegmentPins[i]) >= minimum) segments |= 1 << i;
        return new(Segments: segments);
    }
}
