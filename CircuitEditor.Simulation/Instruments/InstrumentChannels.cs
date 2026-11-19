using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Simulation.Instruments;

internal sealed class InstrumentChannels(InstrumentKind kind)
{
    private const int MeterAndScopeChannels = 2;
    private const int LogicChannels = 8;
    internal WireEndpoint?[] Probes { get; } = new WireEndpoint?[kind == InstrumentKind.Logic ? LogicChannels : MeterAndScopeChannels];
    internal TraceHistory History { get; } = new();
    internal bool Active => Probes.Any(p => p is not null);
}
