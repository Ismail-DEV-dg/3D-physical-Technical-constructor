using CircuitEditor.Core.Breadboards;
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
        foreach (var contact in footprint.Contacts)
        {
            if (!component.Definition.Pins.Any(pin => pin.Id == contact.ElectricalPinId && pin.IsExposed))
                return Fail("Контакт корпуса не соответствует доступному электрическому выводу.");
            var position = ContactGeometry.World(footprint, contact, snapped, board.Definition.HolePitch);
            var hole = Nearest(board, position);
            if (hole is null || Distance(board, hole, position) > PhysicalSettings.HoleSnapToleranceMm)
                return Fail("Не все контакты совпадают с отверстиями. Вставка отменена.");
            if (candidates.Any(binding => binding.HoleId == hole.Id))
                return Fail("Два контакта попали в одно отверстие.");
            if (project.Bindings.Any(binding => binding.BoardId == boardId && binding.HoleId == hole.Id
                && binding.ComponentId != componentId)) return Fail($"Отверстие {hole.Id} занято.");
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
    private static BreadboardHole? Nearest(BreadboardInstance board, LayoutPoint point) =>
        board.Definition.Holes.MinBy(hole => Distance(board, hole, point));
    private static double Distance(BreadboardInstance board, BreadboardHole hole, LayoutPoint point) =>
        Math.Sqrt(Math.Pow(BreadboardGeometry.World(board,hole.Position).X - point.X, 2) + Math.Pow(BreadboardGeometry.World(board,hole.Position).Y - point.Y, 2));
    private static InsertionResult Fail(string error) => new(false, error, null, Array.Empty<ContactBinding>());
}
