using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
namespace CircuitEditor.Core.History;
/// <summary>Независимый снимок экземпляров; библиотечные определения разделяются и не изменяются.</summary>
public sealed class ProjectSnapshot
{
    private readonly CircuitProject state;
    /// <summary>Снимает изменяемое состояние без JSON и файлового ввода-вывода.</summary>
    public ProjectSnapshot(CircuitProject project) => state = Clone(project);
    /// <summary>Создаёт независимый проект для восстановления.</summary>
    public CircuitProject Restore() => Clone(state);
    /// <summary>Сравнивает только данные проекта, исключая выделение и камеру.</summary>
    public bool Matches(CircuitProject other)
    {
        if (state.Circuit.Components.Count != other.Circuit.Components.Count || state.Circuit.Wires.Count != other.Circuit.Wires.Count) return false;
        return state.MockupNotes.SequenceEqual(other.MockupNotes) && state.Breadboards.Select(b=>(b.Id,b.Definition.Id,b.X,b.Y,b.Rotation)).SequenceEqual(other.Breadboards.Select(b=>(b.Id,b.Definition.Id,b.X,b.Y,b.Rotation))) && state.Circuit.Components.Zip(other.Circuit.Components).All(pair => Same(pair.First, pair.Second))
            && state.Circuit.Counters.OrderBy(p => p.Key).SequenceEqual(other.Circuit.Counters.OrderBy(p => p.Key))
            && state.Bindings.SequenceEqual(other.Bindings)
            && SameLayout(state.MockupLayout, other.MockupLayout) && SameLayout(state.SchematicLayout, other.SchematicLayout)
            && state.Circuit.Wires.Zip(other.Circuit.Wires).All(p => p.First.Id == p.Second.Id && p.First.Start == p.Second.Start
                && p.First.End == p.Second.End && p.First.Color == p.Second.Color && p.First.IsStraight == p.Second.IsStraight && p.First.ControlPoints.SequenceEqual(p.Second.ControlPoints));
    }
    private static bool Same(Component a, Component b) => a.Id == b.Id && a.Reference == b.Reference && a.Definition.Id == b.Definition.Id
        && a.Value == b.Value && a.Color == b.Color && a.I2cAddress == b.I2cAddress && a.CapacitorKind == b.CapacitorKind
        && a.PotentiometerAngle == b.PotentiometerAngle && a.SplitterOutputs == b.SplitterOutputs
        && a.Electrical.Parameters.OrderBy(p=>p.Key).SequenceEqual(b.Electrical.Parameters.OrderBy(p=>p.Key))
        && a.Electrical.PinRoles.OrderBy(p=>p.Key).SequenceEqual(b.Electrical.PinRoles.OrderBy(p=>p.Key)) && a.Inputs.Values.OrderBy(p => p.Key).SequenceEqual(b.Inputs.Values.OrderBy(p => p.Key));
    private static bool SameLayout(ViewLayout a, ViewLayout b) => a.Placements.Count == b.Placements.Count && a.Placements.All(p =>
        b.Placements.TryGetValue(p.Key, out var q) && p.Value.X == q.X && p.Value.Y == q.Y && p.Value.Rotation == q.Rotation && p.Value.SpanHoles == q.SpanHoles);
    /// <summary>Копирует состояние компонента с прежним или новым идентификатором.</summary>
    public static Component Copy(Component c, Guid? id = null, string? reference = null) =>
        new(c.Definition, reference ?? c.Reference, id ?? c.Id, c.Value, c.Color, c.I2cAddress, c.CapacitorKind,
            c.PotentiometerAngle, c.Inputs, c.Electrical, c.SplitterOutputs);
    private static CircuitProject Clone(CircuitProject p) => new(new Circuit(p.Circuit.Components.Select(c => Copy(c)).ToArray(), p.Circuit.Wires.ToArray(), p.Circuit.Counters),
        new ViewLayout(RepresentationKind.Mockup, p.MockupLayout.Placements), new ViewLayout(RepresentationKind.Schematic, p.SchematicLayout.Placements),
        p.Breadboards, p.Footprints, p.Bindings, chips: p.Chips, mockupNotes: p.MockupNotes);
}
