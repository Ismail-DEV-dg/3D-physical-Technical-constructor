using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Simulation.Instruments;

public sealed class Multimeter : IInstrument
{
    public InstrumentKind Kind => InstrumentKind.Meter;

    public InstrumentSignal Measure(InstrumentContext context, IReadOnlyList<WireEndpoint?> probes, TraceHistory history) =>
        new(context.Difference(probes[0], probes[1]), context.Current(probes[0], probes[1]));
}
