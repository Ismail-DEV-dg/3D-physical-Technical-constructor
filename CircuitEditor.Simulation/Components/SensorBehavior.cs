using System.Collections.Immutable;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public abstract class SensorBehavior(string componentType, bool powered = false) : IComponentBehavior
{
    public string ComponentType => componentType;

    public ComponentSignal Observe(BehaviorContext context, Component c)
    {
        if (powered && !context.Powered(c)) return new();
        var values = c.Inputs.Values.ToImmutableDictionary(v => v.Key.ToString(), v => v.Value);
        return new(Angle: c.PotentiometerAngle, Measurements: values,
            Active: c.Inputs.Values.GetValueOrDefault(ComponentInput.ButtonPressed) > 0);
    }
}
