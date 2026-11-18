using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.Core.Documents;
/// <summary>Атомарное разрешение библиотечных ID и проверка загруженного документа.</summary>
public static class ProjectDocumentResolver
{
    /// <summary>Создаёт отдельный проект; существующий проект не изменяется при ошибке.</summary>
    public static CircuitProject Resolve(ProjectDocument data, IReadOnlyList<ComponentDefinition> library,
        IReadOnlyList<FootprintDefinition> footprints, BreadboardDefinition board, IComponentInsertionService insertion)
    {
        if (data.SchemaVersion != ProjectDocument.CurrentSchemaVersion) throw new ArgumentException("Неподдерживаемая версия .circ.");
        if (data.LibraryVersion != ProjectDocument.CurrentLibraryVersion) throw new ArgumentException("Версия библиотеки проекта не поддерживается.");
        if (data.Breadboards.Count != 1) throw new ArgumentException("Проект должен содержать одну breadboard.");
        var boards = data.Breadboards.Select(b => b.DefinitionId == board.Id && b.Id != Guid.Empty && double.IsFinite(b.X) && double.IsFinite(b.Y)
            ? new BreadboardInstance(b.Id, board, b.X, b.Y) : throw new ArgumentException($"Не найдена или повреждена breadboard: {b.DefinitionId}.")).ToArray();
        var components = data.Components.Select(c =>
        {
            var definition = library.FirstOrDefault(d => d.Id == c.DefinitionId) ?? throw new ArgumentException($"В библиотеке не найден компонент: {c.DefinitionId}.");
            if (!footprints.Any(f => f.Id == c.FootprintId && f.ComponentDefinitionId == c.DefinitionId))
                throw new ArgumentException($"В библиотеке не найден корпус: {c.FootprintId}.");
            if (c.CapacitorKind is not null && c.DefinitionId != "capacitor" || c.PotentiometerAngle is not null && c.DefinitionId != "potentiometer")
                throw new ArgumentException("Параметр корпуса не относится к этому компоненту.");
            if (c.Color is not null && c.DefinitionId != "led") throw new ArgumentException("Цвет не относится к этому компоненту.");
            return new Component(definition, c.Reference, c.Id, c.Value, c.Color ?? definition.DefaultColor, c.I2cAddress,
                c.CapacitorKind ?? Models.CapacitorKind.Ceramic, c.PotentiometerAngle ?? 0, c.Inputs);
        }).ToArray();
        var ids = components.Select(c => c.Id).ToHashSet();
        if (data.Mockup.Count != ids.Count || data.Schematic.Count != ids.Count || data.Mockup.Keys.Any(id => !ids.Contains(id)) || data.Schematic.Keys.Any(id => !ids.Contains(id)))
            throw new ArgumentException("Повреждены размещения компонентов.");
        foreach (var component in components)
        {
            var footprint = footprints.Single(f => f.ComponentDefinitionId == component.Definition.Id);
            var placement = data.Mockup[component.Id];
            var span = placement.SpanHoles == 0 ? footprint.DefaultSpan : placement.SpanHoles;
            if (!footprint.AllowedRotations.Contains(placement.Rotation) || footprint.IsFlexible && (span < footprint.MinSpan || span > footprint.MaxSpan)
                || !footprint.IsFlexible && placement.SpanHoles != 0)
                throw new ArgumentException("Недопустимые поворот или шаг ножек корпуса.");
        }
        if (data.Counters.Any(p => p.Value < 0)) throw new ArgumentException("Повреждены счётчики обозначений.");
        var project = new CircuitProject(new Circuit(components, counters: data.Counters),
            new ViewLayout(RepresentationKind.Mockup, data.Mockup), new ViewLayout(RepresentationKind.Schematic, data.Schematic), boards, footprints);
        foreach (var group in data.Bindings.GroupBy(b => b.ComponentId))
        {
            var component = components.SingleOrDefault(c => c.Id == group.Key) ?? throw new ArgumentException("Привязка ссылается на неизвестный компонент.");
            var footprint = footprints.Single(f => f.ComponentDefinitionId == component.Definition.Id);
            var result = insertion.Insert(project, component.Id, footprint, group.First().BoardId, data.Mockup[component.Id]);
            if (!result.Success || !project.Bindings.Where(b => b.ComponentId == component.Id).OrderBy(b => b.ContactId)
                .SequenceEqual(group.OrderBy(b => b.ContactId))) throw new ArgumentException("Некорректная вставка или занятые отверстия.");
        }
        if (data.Wires.Select(w => w.Id).Distinct().Count() != data.Wires.Count) throw new ArgumentException("Повторяющийся ID перемычки.");
        foreach (var wire in data.Wires)
        {
            if (WireGeometry.Validate(project, wire) is { } error) throw new ArgumentException(error);
            project.Circuit.PutWire(wire);
        }
        var v = data.View;
        if (!Enum.IsDefined(v.Active) || v.MockupZoom is < ProjectViewState.MinimumZoom or > ProjectViewState.MaximumZoom || v.SchematicZoom is < ProjectViewState.MinimumZoom or > ProjectViewState.MaximumZoom
            || new[] {v.MockupZoom,v.MockupX,v.MockupY,v.SchematicZoom,v.SchematicX,v.SchematicY}.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("Некорректное состояние камеры.");
        return project;
    }
}
