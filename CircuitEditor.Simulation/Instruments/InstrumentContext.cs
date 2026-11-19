using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Instruments;

public sealed class InstrumentContext(SimulationCircuit circuit, long time)
{
    public long Time => time;
    private readonly Core.Electronics.DcResult? instantaneous = circuit.ResultAt(time);

    public double? Voltage(WireEndpoint? endpoint)
    {
        var net = endpoint is null ? null : circuit.Nets.Find(endpoint);
        return net is null ? null : instantaneous?.Voltages.GetValueOrDefault(net.Id);
    }

    public string? Digital(WireEndpoint? endpoint)
    {
        if (endpoint is null || circuit.Nets.Find(endpoint) is not { } net) return null;
        return circuit.ReadDigital(net, Time) switch
        {
            Gpio.DigitalLevel.Low => "0",
            Gpio.DigitalLevel.High => "1",
            Gpio.DigitalLevel.X => "X",
            _ => "Z"
        };
    }

    public double? Difference(WireEndpoint? positive, WireEndpoint? negative)
    {
        if (positive is null || negative is null || circuit.Average is not { } average ||
            circuit.Nets.Find(positive) is not { } a || circuit.Nets.Find(negative) is not { } b ||
            !average.References.TryGetValue(a.Id, out var reference) || average.References.GetValueOrDefault(b.Id) != reference)
            return null;
        return average.Voltages.GetValueOrDefault(a.Id) - average.Voltages.GetValueOrDefault(b.Id);
    }

    public double? Current(WireEndpoint? positive, WireEndpoint? negative)
    {
        if (positive is null || negative is null) return null;
        var wire = circuit.Project.Circuit.Wires.FirstOrDefault(w => w.Start == positive && w.End == negative ||
            w.End == positive && w.Start == negative);
        if (wire is not null)
            return (wire.Start == positive ? 1 : -1) * circuit.Wires().GetValueOrDefault(wire.Id)?.Current;
        if (positive.Kind != WireEndpointKind.ComponentContact || negative.Kind != WireEndpointKind.ComponentContact ||
            positive.OwnerId != negative.OwnerId || circuit.Average is not { } average) return null;
        var part = circuit.Project.Circuit.Components.FirstOrDefault(c => c.Id == positive.OwnerId);
        if (part is null) return null;
        var footprint = circuit.Project.Footprints.First(f => f.ComponentDefinitionId == part.Definition.Id);
        var a = footprint.Contacts.FirstOrDefault(c => c.Id == positive.TerminalId)?.ElectricalPinId;
        var b = footprint.Contacts.FirstOrDefault(c => c.Id == negative.TerminalId)?.ElectricalPinId;
        if (a is null || b is null || a == b) return null;
        var branches = average.Branches.Where(branch => branch.ComponentId == part.Id &&
            (branch.PinA == a && branch.PinB == b || branch.PinA == b && branch.PinB == a)).ToArray();
        return branches.Length == 1 ? (branches[0].PinA == a ? 1 : -1) * branches[0].Current : null;
    }
}
