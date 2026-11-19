using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class ServoBehavior : IComponentBehavior
{
    public IReadOnlyList<string> ComponentTypes => ["servo"];
    private const double FullAngle = 180, MicrosecondsPerSecond = 1_000_000;

    public ComponentSignal Observe(BehaviorContext context, Component c)
    {
        var memory = context.For(c);
        if (context.Powered(c) && context.PulseWidth(c, "SIG") is double pulse)
        {
            var minimum = context.Parameter(c, "minimumPulseUs");
            var maximum = context.Parameter(c, "maximumPulseUs");
            memory.TargetAngle = FullAngle * Math.Clamp((pulse - minimum) / (maximum - minimum), 0, 1);
        }

        if (context.Powered(c))
        {
            var travel = context.Parameter(c, "degreesPerSecond") * context.Elapsed / MicrosecondsPerSecond;
            memory.Angle += Math.Clamp(memory.TargetAngle - memory.Angle, -travel, travel);
        }
        return new(Angle: memory.Angle);
    }
}
