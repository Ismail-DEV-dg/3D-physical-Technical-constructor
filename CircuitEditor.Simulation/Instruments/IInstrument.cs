using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Simulation.Instruments;

public interface IInstrument
{
    InstrumentKind Kind { get; }
    InstrumentSignal Measure(InstrumentContext context, IReadOnlyList<WireEndpoint?> probes, TraceHistory history);
    void Sample(InstrumentContext context, IReadOnlyList<WireEndpoint?> probes, TraceHistory history) { }
}
