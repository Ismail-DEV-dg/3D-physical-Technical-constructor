using System.Collections.Immutable;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Engine;

internal static class PinMeasurements
{
    internal static ImmutableDictionary<PinReference,PinSignal> Read(SimulationCircuit circuit, long time)
    {
        var snapshot = ImmutableDictionary.CreateBuilder<PinReference,PinSignal>();
        foreach (var board in circuit.Controllers.Values)
            foreach (var p in board.Pins)
            {
                var pin = new PinReference(board.Id, p.Id);
                var voltage = circuit.Voltage(pin);
                var level = circuit.ReadDigital(pin, time) switch
                {
                    DigitalLevel.High => "1", DigitalLevel.Low => "0", DigitalLevel.Z => "Z", _ => "X"
                };
                snapshot.Add(pin, new(level, board.ReadAdc(p.Id, voltage), voltage));
            }
        return snapshot.ToImmutable();
    }
}
