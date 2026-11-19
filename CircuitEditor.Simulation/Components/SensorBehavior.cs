using System.Collections.Immutable;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class SensorBehavior : IComponentBehavior
{
    public IReadOnlyList<string> ComponentTypes => ["dht11", "dht22", "photoresistor", "thermistor", "potentiometer", "button", "switch"];

    public ComponentSignal Observe(BehaviorContext context, Component c)
    {
        if (c.Definition.Id is "dht11" or "dht22" && !context.Powered(c)) return new();
        var values = c.Inputs.Values.ToImmutableDictionary(v => v.Key.ToString(), v => v.Value);
        return new(Angle: c.PotentiometerAngle, Measurements: values,
            Active: c.Inputs.Values.GetValueOrDefault(ComponentInput.ButtonPressed) > 0);
    }
}
