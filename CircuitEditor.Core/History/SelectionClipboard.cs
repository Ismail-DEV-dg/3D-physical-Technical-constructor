using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.Core.History;
/// <summary>Внутренняя копия выбранных экземпляров и перемычек только между ними.</summary>
public sealed class SelectionClipboard
{
    private readonly CircuitProject source;
    private readonly HashSet<Guid> selected;
    private int pasteCount;
    private const double PasteOffsetMm = 2 * Physical.PhysicalSettings.FreeGridMm;
    private const double SchematicOffset = 2 * Geometry.DiagramGeometry.GridSize;
    /// <summary>Создаёт независимый буфер без ссылок на отверстия и внешние компоненты.</summary>
    public SelectionClipboard(CircuitProject project, IEnumerable<Guid> ids)
    { source = new ProjectSnapshot(project).Restore(); selected = ids.ToHashSet(); }
    /// <summary>Вставляет свободно слева от breadboard; нумерация и ID новые.</summary>
    public IReadOnlyList<Guid> Paste(CircuitProject target)
    {
        var map = new Dictionary<Guid, Guid>(); var copies = new List<Component>(); pasteCount++;
        var originals = source.Circuit.Components.Where(c => selected.Contains(c.Id)).ToArray();
        if (originals.Length == 0) return [];
        var minY = originals.Min(c => source.MockupLayout.Get(c.Id).Y);
        var maxX = originals.Max(c => { var p = source.MockupLayout.Get(c.Id); var f = source.Footprints.Single(f => f.ComponentDefinitionId == c.Definition.Id);
            return p.X + Physical.ContactGeometry.Bounds(f,p,Physical.PhysicalSettings.FreeGridMm).Right; });
        var right = target.Breadboards.Min(b => b.X) - Physical.PhysicalSettings.FreeGridMm * 2 - pasteCount * PasteOffsetMm;
        var dx = right - maxX; var dy = PasteOffsetMm * pasteCount - minY;
        foreach (var original in originals)
        {
            if (source.FindChip(original.Definition.Id) is { } chip) target.RegisterChip(chip);
            var allocated = target.AddComponent(original.Definition);
            var copy = ProjectSnapshot.Copy(original, allocated.Id, allocated.Reference);
            target.RemoveComponent(allocated.Id); copies.Add(copy); map.Add(original.Id, copy.Id);
            var m = source.MockupLayout.Get(original.Id); var s = source.SchematicLayout.Get(original.Id);
            target.MockupLayout.Set(copy.Id, new(m.X + dx, m.Y + dy, m.Rotation, m.SpanHoles));
            target.SchematicLayout.Set(copy.Id, new(s.X + pasteCount * SchematicOffset, s.Y + pasteCount * SchematicOffset, s.Rotation, s.SpanHoles));
        }
        var wires = target.Circuit.Wires.ToList();
        foreach (var wire in source.Circuit.Wires.Where(w => w.Start.Kind == WireEndpointKind.ComponentContact && w.End.Kind == WireEndpointKind.ComponentContact
            && map.ContainsKey(w.Start.OwnerId) && map.ContainsKey(w.End.OwnerId)))
            wires.Add(new Wire(WireEndpoint.Contact(map[wire.Start.OwnerId], wire.Start.TerminalId), WireEndpoint.Contact(map[wire.End.OwnerId], wire.End.TerminalId),
                wire.Color, wire.ControlPoints.Select(p => new Layout.LayoutPoint(p.X + dx, p.Y + dy)).ToArray()));
        var state = new CircuitProject(new Circuit(target.Circuit.Components.Concat(copies).ToArray(), wires, target.Circuit.Counters),
            target.MockupLayout, target.SchematicLayout, target.Breadboards, target.Footprints, target.Bindings, chips: target.Chips);
        target.Restore(state); return copies.Select(c => c.Id).ToArray();
    }
}
