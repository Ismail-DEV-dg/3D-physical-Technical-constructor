using System.Collections.Immutable;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Simulation.Engine;

internal sealed class WireMeasurements(CircuitProject project, INetlistBuilder builder)
{
    private readonly Dictionary<Guid, HashSet<string>?> partitions = [];
    private Netlist? unwired;
    private Dictionary<WireEndpoint, string> origins = [];

    internal void Rebuild()
    {
        var snapshot = new ProjectSnapshot(project).Restore();
        foreach (var id in snapshot.Circuit.Wires.Select(w => w.Id).ToArray()) snapshot.Circuit.RemoveWire(id);
        unwired = builder.Build(snapshot);
        origins = unwired.Nets.SelectMany(n => n.Terminals.Where(t => t.Kind is "c" or "h").Select(t =>
            (End: t.Kind == "c" ? WireEndpoint.Contact(t.OwnerId, t.TerminalId) : WireEndpoint.Hole(t.OwnerId, t.TerminalId),
            Node: n.Id))).ToDictionary(p => p.End, p => p.Node);
        var edges = project.Circuit.Wires.Where(w => origins.ContainsKey(w.Start) && origins.ContainsKey(w.End)).ToArray();
        var incident = edges.SelectMany(w => new[] { (origins[w.Start], w), (origins[w.End], w) })
            .ToLookup(p => p.Item1, p => p.w);
        partitions.Clear();
        foreach (var wire in edges)
        {
            var from = origins[wire.Start];
            var to = origins[wire.End];
            var visited = new HashSet<string> { from };
            var queue = new Queue<string>();
            queue.Enqueue(from);
            while (queue.TryDequeue(out var node) && !visited.Contains(to))
            {
                foreach (var edge in incident[node].Where(e => e.Id != wire.Id))
                {
                    var adjacent = node == origins[edge.Start] ? origins[edge.End] : origins[edge.Start];
                    if (visited.Add(adjacent)) queue.Enqueue(adjacent);
                }
            }
            partitions.Add(wire.Id, visited.Contains(to) ? null : visited);
        }
    }

    internal ImmutableDictionary<Guid,WireSignal> Read(Netlist nets, DcResult? result)
    {
        if (project.Circuit.Wires.Count == 0 || result is null) return ImmutableDictionary<Guid,WireSignal>.Empty;
        if (unwired is null) Rebuild();
        var nodeByEnd = nets.Nets.SelectMany(n => n.Terminals.Where(t => t.Kind is "c" or "h").Select(t =>
            (End: t.Kind == "c" ? WireEndpoint.Contact(t.OwnerId, t.TerminalId) : WireEndpoint.Hole(t.OwnerId, t.TerminalId),
            Node: n.Id))).ToDictionary(p => p.End, p => p.Node);
        var values = ImmutableDictionary.CreateBuilder<Guid,WireSignal>();
        foreach (var wire in project.Circuit.Wires)
        {
            if (!nodeByEnd.TryGetValue(wire.Start, out var node)) continue;
            var voltage = result.Voltages.GetValueOrDefault(node);
            double? amps = null;
            if (voltage is not null && partitions.GetValueOrDefault(wire.Id) is { } cut)
            {
                amps = 0;
                foreach (var branch in result.Branches)
                {
                    var atStart = branch.A == node && branch.PinA is { } a &&
                        unwired!.Find(branch.ComponentId, a) is { } start && cut.Contains(start.Id);
                    var atEnd = branch.B == node && branch.PinB is { } b &&
                        unwired!.Find(branch.ComponentId, b) is { } end && cut.Contains(end.Id);
                    if (atStart != atEnd) amps += (atStart ? -1 : 1) * branch.Current;
                }
            }
            values.Add(wire.Id, new(voltage, amps));
        }
        return values.ToImmutable();
    }

    internal void Invalidate() => unwired = null;
}
