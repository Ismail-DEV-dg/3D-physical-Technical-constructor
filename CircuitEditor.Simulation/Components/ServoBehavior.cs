using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class ServoBehavior : IComponentBehavior
{
    public string ComponentType => "servo";
    private const double FullAngle = 180, MicrosecondsPerSecond = 1_000_000;

    public bool Update(BehaviorContext context, Component c)
    {
        var memory = context.For(c);
        if (memory.Powered)
        {
            var travel = context.Parameter(c, "degreesPerSecond") *
                (context.Now - memory.AngleUpdatedAt) / MicrosecondsPerSecond;
            memory.Angle += Math.Clamp(memory.TargetAngle - memory.Angle, -travel, travel);
        }

        memory.AngleUpdatedAt = context.Now;
        memory.Powered = context.Powered(c);
        if (memory.Powered && context.PulseWidth(c, "SIG") is double pulse)
        {
            var minimum = context.Parameter(c, "minimumPulseUs");
            var maximum = context.Parameter(c, "maximumPulseUs");
            memory.TargetAngle = FullAngle * Math.Clamp((pulse - minimum) / (maximum - minimum), 0, 1);
        }
        return false;
    }

    public ComponentSignal Observe(BehaviorContext context, Component c) => new(Angle: context.For(c).Angle);
}
