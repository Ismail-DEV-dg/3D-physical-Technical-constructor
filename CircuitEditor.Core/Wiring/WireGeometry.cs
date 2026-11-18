using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
namespace CircuitEditor.Core.Wiring;
/// <summary>Разрешение физических концов и проверка перемычек без UI.</summary>
public static class WireGeometry
{
    /// <summary>Координаты конца; вычисляются заново, поэтому следуют за компонентом.</summary>
    public static LayoutPoint? Resolve(CircuitProject project, WireEndpoint endpoint)
    {
        if (endpoint.Kind == WireEndpointKind.BreadboardHole)
        {
            var board = project.Breadboards.FirstOrDefault(b => b.Id == endpoint.OwnerId);
            var hole = board?.Definition.Holes.FirstOrDefault(h => h.Id == endpoint.TerminalId);
            return board is not null && hole is not null ? Breadboards.BreadboardGeometry.World(board,hole.Position) : null;
        }
        var component = project.Circuit.Components.FirstOrDefault(c => c.Id == endpoint.OwnerId);
        var footprint = project.Footprints.FirstOrDefault(f => f.ComponentDefinitionId == component?.Definition.Id);
        var contact = footprint?.Contacts.FirstOrDefault(c => c.Id == endpoint.TerminalId);
        return component is not null && footprint is not null && contact is not null
            ? ContactGeometry.World(footprint, contact, project.MockupLayout.Get(component.Id), PhysicalSettings.FreeGridMm) : null;
    }
    /// <summary>Атомарная проверка существования, длины и дубликатов.</summary>
    public static string? Validate(CircuitProject project, Wire wire)
    {
        var a = Resolve(project, wire.Start); var b = Resolve(project, wire.End);
        if (a is null || b is null) return "Конец должен находиться на физическом контакте или отверстии.";
        if (a == b) return "Нулевая длина перемычки недопустима.";
        if (project.Circuit.Wires.Any(w => w.Id != wire.Id &&
            (w.Start == wire.Start && w.End == wire.End || w.Start == wire.End && w.End == wire.Start)))
            return "Такое соединение уже существует.";
        return null;
    }
    /// <summary>Ближайшая допустимая точка в общем допуске привязки.</summary>
    public static WireEndpoint? Nearest(CircuitProject project, LayoutPoint point)
    {
        var candidates = project.Circuit.Components.SelectMany(c => project.Footprints.Where(f => f.ComponentDefinitionId == c.Definition.Id)
            .SelectMany(f => f.Contacts.Select(p => WireEndpoint.Contact(c.Id, p.Id))))
            .Concat(project.Breadboards.SelectMany(b => b.Definition.Holes.Select(h => WireEndpoint.Hole(b.Id, h.Id))));
        WireEndpoint? nearest = null; var distance = PhysicalSettings.HoleCaptureRadiusMm * PhysicalSettings.HoleCaptureRadiusMm;
        foreach (var endpoint in candidates)
        {
            if (Resolve(project, endpoint) is not { } p) continue;
            var squared = (p.X - point.X) * (p.X - point.X) + (p.Y - point.Y) * (p.Y - point.Y);
            if (squared < distance) { nearest = endpoint; distance = squared; }
        }
        return nearest;
    }
}
