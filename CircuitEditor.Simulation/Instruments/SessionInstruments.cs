using System.Collections.Immutable;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Instruments;

internal sealed class SessionInstruments(SimulationCircuit circuit, IEnumerable<IInstrument> registry)
{
    internal const long SampleMicroseconds = 100;
    private readonly Dictionary<InstrumentKind, IInstrument> instruments = registry.ToDictionary(i => i.Kind);
    private readonly Dictionary<InstrumentKind, InstrumentChannels> channels = Enum.GetValues<InstrumentKind>()
        .ToDictionary(kind => kind, kind => new InstrumentChannels(kind));

    internal bool IsRecording => channels.Any(p => p.Key != InstrumentKind.Meter && p.Value.Active);

    internal bool Assign(InstrumentKind kind, int channel, WireEndpoint? terminal)
    {
        if (!channels.TryGetValue(kind, out var config) || channel < 0 || channel >= config.Probes.Length ||
            terminal is not null && circuit.Nets.Find(terminal) is null) return false;
        if (config.Probes[channel] == terminal) return true;
        config.Probes[channel] = terminal;
        config.History.Clear();
        return true;
    }

    internal void Sample(long time)
    {
        var context = new InstrumentContext(circuit, time);
        foreach (var (kind, instrument) in instruments)
        {
            var config = channels[kind];
            if (kind != InstrumentKind.Meter && config.Active) instrument.Sample(context, config.Probes, config.History);
        }
    }

    internal ImmutableDictionary<InstrumentKind, InstrumentSignal> Capture(long time)
    {
        var context = new InstrumentContext(circuit, time);
        return instruments.Where(p => channels[p.Key].Active)
            .ToImmutableDictionary(p => p.Key, p => p.Value.Measure(context, channels[p.Key].Probes, channels[p.Key].History));
    }
}
