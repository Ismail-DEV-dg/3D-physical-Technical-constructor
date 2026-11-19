using System.Collections.Immutable;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Components;

public sealed class UltrasonicBehavior : IComponentBehavior
{
    public string ComponentType => "hc-sr04";

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component c) => !context.For(c).Powered ? [] :
        context.Branch(c, "ECHO", "ECHO", "GND", "TH", context.Parameter(c, "outputOhms"),
            context.For(c).Echo ? context.Voltage(c, "VCC", "GND") ?? 0 : 0);

    public bool Update(BehaviorContext context, Component c)
    {
        var memory = context.For(c);
        var powered = context.Powered(c);
        var changed = powered != memory.Powered;
        memory.Powered = powered;
        if (!powered)
        {
            memory.TriggerStart = null;
            return changed;
        }
        var level = context.Level(c, "TRIG");
        if (level == memory.Level) return changed;
        if (level == DigitalLevel.High) memory.TriggerStart = context.Now;
        if (level == DigitalLevel.Low && memory.TriggerStart is long start &&
            context.Now - start >= context.Parameter(c, "minimumTriggerUs") && !memory.Echo)
        {
            memory.Echo = true;
            var width = EchoWidth(context, c);
            context.ChangeAt(context.Now + width, () => memory.Echo = false);
            changed = true;
        }
        if (level != DigitalLevel.High) memory.TriggerStart = null;
        memory.Level = level;
        return changed;
    }

    public ComponentSignal Observe(BehaviorContext context, Component c) => new(
        Measurements: context.Powered(c) ? ImmutableDictionary<string,double>.Empty
            .Add("Distance", c.Inputs.Values.GetValueOrDefault(ComponentInput.Distance))
            .Add("EchoUs", EchoWidth(context, c)) : null);

    private static long EchoWidth(BehaviorContext context, Component c) =>
        (long)Math.Round(c.Inputs.Values.GetValueOrDefault(ComponentInput.Distance) * context.Parameter(c, "echoUsPerCm"));
}
