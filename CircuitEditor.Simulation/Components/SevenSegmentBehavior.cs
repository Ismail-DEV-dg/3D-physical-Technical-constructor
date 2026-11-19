using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Components;

public sealed class SevenSegmentBehavior : IComponentBehavior
{
    private static readonly string[] SegmentPins = ["A", "B", "C", "D", "E", "F", "G", "DP"];
    public IReadOnlyList<string> ComponentTypes => ["seven-segment"];

    public ComponentSignal Observe(BehaviorContext context, Component c)
    {
        var segments = 0;
        if (context.Level(c, "COM") == DigitalLevel.Low)
            for (var i = 0; i < SegmentPins.Length; i++)
                if (context.Level(c, SegmentPins[i]) == DigitalLevel.High) segments |= 1 << i;
        return new(Segments: segments);
    }
}
