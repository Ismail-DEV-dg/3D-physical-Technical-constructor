using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Simulation;
namespace CircuitEditor.Core.Documents;
/// <summary>Контракт .circ: ссылки на штатную библиотеку и копии пользовательских определений.</summary>
public sealed record ProjectDocument
{
    /// <summary>Текущая версия формата со скетчем и настройками симуляции.</summary>
    public const int CurrentSchemaVersion = 4;
    /// <summary>Текст скетча и выбранная плата.</summary>
    public Code.SketchDocument Sketch { get; init; } = new();
    /// <summary>Сценарий и положения приборов, без данных активного сеанса.</summary>
    public SimulationSetup SimulationSetup { get; init; } = new();
    /// <summary>Версия установленной библиотеки.</summary>
    public const string CurrentLibraryVersion = "1";
    /// <summary>Версия формата.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    /// <summary>Версия библиотеки.</summary>
    public string LibraryVersion { get; init; } = CurrentLibraryVersion;
    /// <summary>Электрические экземпляры.</summary>
    public IReadOnlyList<ComponentData> Components { get; init; } = [];
    /// <summary>Одна сохранённая копия на ID и ревизию размещённой микросхемы.</summary>
    public IReadOnlyList<ChipDefinition> EmbeddedChips { get; init; } = [];
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
    /// <summary>Подписи на физическом макете.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MockupNote>? MockupNotes { get; init; }

    /// <summary>Активный режим и камеры.</summary>
    public ProjectViewState View { get; init; } = new();
    /// <summary>Снимает документ со ссылками на штатные компоненты и копиями пользовательских микросхем.</summary>
    public static ProjectDocument Capture(CircuitProject project, ProjectViewState? view = null) => new()
    {
        Sketch = project.Sketch,
        MockupNotes = project.MockupNotes.Count == 0 ? null : project.MockupNotes.ToArray(),
        SimulationSetup = project.SimulationSetup,
        EmbeddedChips = project.Circuit.Components.Select(part => project.FindChip(part.Definition.Id))
            .OfType<ChipDefinition>().DistinctBy(chip => chip.ComponentId).ToArray(),
        Components = project.Circuit.Components.Select(c => new ComponentData(c.Id, c.Definition.Id,
            project.Footprints.Single(f => f.ComponentDefinitionId == c.Definition.Id).Id, c.Reference, c.Value,
            c.Definition.Id == "led" ? c.Color : null, c.I2cAddress,
            c.Definition.Id == "capacitor" ? c.CapacitorKind : null,
            c.Definition.Id == "potentiometer" ? c.PotentiometerAngle : null, c.Inputs.Values.Count > 0 ? c.Inputs : null, c.Electrical.Parameters.Count>0 || c.Electrical.PinRoles.Count>0 ? c.Electrical : null,
            project.FindChip(c.Definition.Id)?.Id, project.FindChip(c.Definition.Id)?.DefinitionVersion)).ToArray(),
        Counters = project.Circuit.Counters.ToDictionary(), Mockup = project.MockupLayout.Placements.ToDictionary(p => p.Key, p => p.Value.Copy()),
        Schematic = project.SchematicLayout.Placements.ToDictionary(p => p.Key, p => p.Value.Copy()),
        Breadboards = project.Breadboards.Select(b => new BreadboardData(b.Id, b.Definition.Id, b.X, b.Y, b.Rotation)).ToArray(),
        Bindings = project.Bindings.ToArray(), Wires = project.Circuit.Wires.ToArray(), View = view ?? new()
    };
}
