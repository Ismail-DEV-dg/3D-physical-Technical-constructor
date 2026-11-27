using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Core.Services;

/// <summary>Проверяет все контакты и занятость до единственной атомарной фиксации.</summary>
public sealed class ComponentInsertionService : IComponentInsertionService
{
    /// <inheritdoc/>
    public InsertionResult Preview(CircuitProject project, Guid componentId, FootprintDefinition footprint,
        Guid boardId, ComponentPlacement proposed)
    {
        var component = project.Circuit.Components.FirstOrDefault(item => item.Id == componentId);
        var board = project.Breadboards.FirstOrDefault(item => item.Id == boardId);
        if (component is null || board is null || component.Definition.Id != footprint.ComponentDefinitionId)
            return Fail("Не найден компонент, корпус или макетная плата.");
        return Preview(project, component.Definition, componentId, footprint, board, proposed);
    }

    /// <inheritdoc/>
    public InsertionResult PreviewNew(CircuitProject project, ComponentDefinition definition, FootprintDefinition footprint,
        Guid boardId, LayoutPoint pointer, int? rotation = null)
    {
        var board = project.Breadboards.FirstOrDefault(item => item.Id == boardId);
        if (board is null || definition.Id != footprint.ComponentDefinitionId || footprint.Contacts.Count == 0)
            return Fail("Не найден корпус или макетная плата.");
        var local = BreadboardGeometry.Local(board, pointer);
        var radius = 2 * board.Definition.HolePitch;
        var anchors = board.Definition.Holes.Select(hole => (Hole: hole, Distance: DistanceSquared(hole.Position, local)))
            .Where(item => item.Distance <= radius * radius).OrderBy(item => item.Distance);
        var rotations = footprint.AllowedRotations.Where(angle => rotation is null || angle == rotation)
            .OrderBy(angle => (angle - board.Rotation + ComponentPlacement.FullRotation) % ComponentPlacement.FullRotation).ToArray();
        var groups = board.Definition.Holes.ToDictionary(hole => hole.Id, hole => hole.GroupId);
        var pins = footprint.Contacts.ToDictionary(contact => contact.Id, contact => contact.ElectricalPinId);
        var disconnected = definition.Pins.Where(pin => pin.IsNoConnect).Select(pin => pin.Id).ToHashSet();
        InsertionResult? nearestFailure = null;
        foreach (var (hole, _) in anchors)
        {
            var world = BreadboardGeometry.World(board, hole.Position);
            InsertionResult? mechanicallyValid = null;
            foreach (var angle in rotations)
            {
                var pose = new ComponentPlacement(rotation: angle);
                var first = ContactGeometry.Local(footprint, footprint.Contacts[0], pose, board.Definition.HolePitch);
                pose.MoveTo(world.X - first.X, world.Y - first.Y);
                var result = Preview(project, definition, Guid.Empty, footprint, board, pose);
                if (!result.Success) { nearestFailure ??= result; continue; }
                if (rotation.HasValue || result.Bindings.Where(binding => !disconnected.Contains(pins[binding.ContactId]))
                    .GroupBy(binding => groups[binding.HoleId]).All(group => group.Select(binding => pins[binding.ContactId]).Distinct().Count() == 1))
                    return result;
                mechanicallyValid ??= result;
            }
            if (mechanicallyValid is not null) return mechanicallyValid;
        }
        return nearestFailure ?? Fail("Подведите курсор ближе к свободным отверстиям.");
    }

    private static InsertionResult Preview(CircuitProject project, ComponentDefinition definition, Guid componentId,
        FootprintDefinition footprint, BreadboardInstance board, ComponentPlacement proposed)
    {
        var boardId = board.Id;
        if (footprint.Mounting != "breadboard" || footprint.Contacts.Count == 0)
            return Fail("Этот корпус размещается рядом с платой и не вставляется в отверстия.");
        if (!footprint.AllowedRotations.Contains(proposed.Rotation)) return Fail("Недопустимый поворот корпуса.");
        var span = proposed.SpanHoles == 0 ? footprint.DefaultSpan : proposed.SpanHoles;
        if (footprint.IsFlexible && (span < footprint.MinSpan || span > footprint.MaxSpan))
            return Fail($"Шаг ножек: от {footprint.MinSpan} до {footprint.MaxSpan} отверстий.");
        if (!footprint.IsFlexible && proposed.SpanHoles != 0) return Fail("У этого корпуса фиксированные контакты.");
        var first = ContactGeometry.World(footprint, footprint.Contacts[0], proposed, board.Definition.HolePitch);
        var anchor = Nearest(board, first);
        if (anchor is null || Distance(board, anchor, first) > PhysicalSettings.HoleSnapToleranceMm)
            return Fail("Совместите первый контакт с отверстием.");
        var snapped = new ComponentPlacement(proposed.X + BreadboardGeometry.World(board,anchor.Position).X - first.X,
            proposed.Y + BreadboardGeometry.World(board,anchor.Position).Y - first.Y, proposed.Rotation, footprint.IsFlexible ? span : 0);
        var candidates = new List<ContactBinding>();
        var dipGroups = footprint.Renderer == ChipPackage.Renderer ? new HashSet<string>(StringComparer.Ordinal) : null;
        foreach (var contact in footprint.Contacts)
        {
            if (!definition.Pins.Any(pin => pin.Id == contact.ElectricalPinId && pin.IsExposed))
                return Fail("Контакт корпуса не соответствует доступному электрическому выводу.");
            var position = ContactGeometry.World(footprint, contact, snapped, board.Definition.HolePitch);
            var hole = Nearest(board, position);
            if (hole is null || Distance(board, hole, position) > PhysicalSettings.HoleSnapToleranceMm)
                return Fail("Не все контакты совпадают с отверстиями. Вставка отменена.");
            if (candidates.Any(binding => binding.HoleId == hole.Id))
                return Fail("Два контакта попали в одно отверстие.");
            if (project.Bindings.Any(binding => binding.BoardId == boardId && binding.HoleId == hole.Id
                && binding.ComponentId != componentId)) return Fail($"Отверстие {hole.Id} занято.");
            if (dipGroups is not null && !dipGroups.Add(hole.GroupId))
                return Fail("Вставьте DIP поперёк центрального канала, чтобы выводы не замыкались.");
            candidates.Add(new ContactBinding(componentId, contact.Id, boardId, hole.Id));
        }
        return new InsertionResult(true, null, snapped, candidates.AsReadOnly());
    }
    /// <inheritdoc/>
    public InsertionResult Insert(CircuitProject project, Guid componentId, FootprintDefinition footprint,
        Guid boardId, ComponentPlacement proposed)
    {
        var result = Preview(project, componentId, footprint, boardId, proposed);
        if (result.Success) project.CommitInsertion(componentId, result.Placement!, result.Bindings);
        return result;
    }
    private static BreadboardHole? Nearest(BreadboardInstance board, LayoutPoint point)
    {
        var local = BreadboardGeometry.Local(board, point);
        return board.Definition.Holes.MinBy(hole => DistanceSquared(hole.Position, local));
    }
    private static double Distance(BreadboardInstance board, BreadboardHole hole, LayoutPoint point) =>
        Math.Sqrt(DistanceSquared(hole.Position, BreadboardGeometry.Local(board, point)));
    private static double DistanceSquared(LayoutPoint a, LayoutPoint b) =>
        (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    private static InsertionResult Fail(string error) => new(false, error, null, Array.Empty<ContactBinding>());
}
