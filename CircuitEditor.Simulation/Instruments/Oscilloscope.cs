using System.Collections.Immutable;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Simulation.Instruments;

public sealed class Oscilloscope : IInstrument
{
    public InstrumentKind Kind => InstrumentKind.Scope;

    public void Sample(InstrumentContext context, IReadOnlyList<WireEndpoint?> probes, TraceHistory history) =>
        history.Add(new(context.Time, probes.Select(context.Voltage).ToImmutableArray(), []));

    public InstrumentSignal Measure(InstrumentContext context, IReadOnlyList<WireEndpoint?> probes, TraceHistory history) =>
        new(History: history.Snapshot());
}
