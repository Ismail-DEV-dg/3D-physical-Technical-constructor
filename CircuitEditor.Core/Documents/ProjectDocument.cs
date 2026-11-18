using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
namespace CircuitEditor.Core.Documents;
/// <summary>Контракт .circ: только экземпляры и ссылки на библиотечные определения.</summary>
public sealed record ProjectDocument
{
    /// <summary>Первая версия формата; существовавших файлов до неё нет.</summary>
    public const int CurrentSchemaVersion = 1;
    /// <summary>Версия установленной библиотеки.</summary>
    public const string CurrentLibraryVersion = "1";
    /// <summary>Версия формата.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    /// <summary>Версия библиотеки.</summary>
    public string LibraryVersion { get; init; } = CurrentLibraryVersion;
    /// <summary>Электрические экземпляры.</summary>
    public IReadOnlyList<ComponentData> Components { get; init; } = [];
    /// <summary>Счётчики обозначений, включая удалённые.</summary>
    public IReadOnlyDictionary<string, int> Counters { get; init; } = new Dictionary<string, int>();
    /// <summary>Физические и символьные координаты.</summary>
    public IReadOnlyDictionary<Guid, ComponentPlacement> Mockup { get; init; } = new Dictionary<Guid, ComponentPlacement>();
    /// <summary>Символьные координаты.</summary>
    public IReadOnlyDictionary<Guid, ComponentPlacement> Schematic { get; init; } = new Dictionary<Guid, ComponentPlacement>();
    /// <summary>Экземпляры плат.</summary>
    public IReadOnlyList<BreadboardData> Breadboards { get; init; } = [];
    /// <summary>Вставленные контакты.</summary>
    public IReadOnlyList<ContactBinding> Bindings { get; init; } = [];
    /// <summary>Перемычки.</summary>
    public IReadOnlyList<Wire> Wires { get; init; } = [];
    /// <summary>Активный режим и камеры.</summary>
    public ProjectViewState View { get; init; } = new();
    /// <summary>Снимает документ без библиотечных определений.</summary>
    public static ProjectDocument Capture(CircuitProject project, ProjectViewState? view = null) => new()
    {
        Components = project.Circuit.Components.Select(c => new ComponentData(c.Id, c.Definition.Id,
            project.Footprints.Single(f => f.ComponentDefinitionId == c.Definition.Id).Id, c.Reference, c.Value,
            c.Definition.Id == "led" ? c.Color : null, c.I2cAddress,
            c.Definition.Id == "capacitor" ? c.CapacitorKind : null,
            c.Definition.Id == "potentiometer" ? c.PotentiometerAngle : null, c.Inputs.Values.Count > 0 ? c.Inputs : null, c.Electrical.Parameters.Count>0 || c.Electrical.PinRoles.Count>0 ? c.Electrical : null)).ToArray(),
        Counters = project.Circuit.Counters.ToDictionary(), Mockup = project.MockupLayout.Placements.ToDictionary(p => p.Key, p => p.Value.Copy()),
        Schematic = project.SchematicLayout.Placements.ToDictionary(p => p.Key, p => p.Value.Copy()),
        Breadboards = project.Breadboards.Select(b => new BreadboardData(b.Id, b.Definition.Id, b.X, b.Y)).ToArray(),
        Bindings = project.Bindings.ToArray(), Wires = project.Circuit.Wires.ToArray(), View = view ?? new()
    };
}
