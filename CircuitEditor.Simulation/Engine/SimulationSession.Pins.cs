using System.Collections.Immutable;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Engine;

public sealed partial class SimulationSession
{
    private ImmutableDictionary<PinReference,PinSignal> ReadPins()
    {
        var snapshot = ImmutableDictionary.CreateBuilder<PinReference,PinSignal>();
        foreach (var board in Circuit.Controllers.Values)
            foreach (var p in board.Pins)
            {
                var pin = new PinReference(board.Id, p.Id);
                var voltage = Circuit.Voltage(pin);
                var level = Circuit.ReadDigital(pin, clock.Microseconds) switch
                {
                    DigitalLevel.High => "1", DigitalLevel.Low => "0", DigitalLevel.Z => "Z", _ => "X"
                };
                snapshot.Add(pin, new(level, board.ReadAdc(p.Id, voltage), voltage));
            }
        return snapshot.ToImmutable();
    }
}
