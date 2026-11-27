using System.Text.Json.Serialization;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Core.Models;

/// <summary>Одна электрическая модель, две раскладки, корпуса и привязки breadboard.</summary>
public sealed class CircuitProject
{
    private readonly List<ContactBinding> bindings;
    private readonly List<ChipDefinition> chips;
    /// <summary>Создаёт или восстанавливает полный проект без файлового ввода-вывода.</summary>
    [JsonConstructor]
    public CircuitProject(Circuit? circuit = null, ViewLayout? mockupLayout = null, ViewLayout? schematicLayout = null,
        IReadOnlyList<BreadboardInstance>? breadboards = null, IReadOnlyList<FootprintDefinition>? footprints = null,
        IReadOnlyList<ContactBinding>? bindings = null, Code.SketchDocument? sketch = null, SimulationSetup? simulationSetup = null,
        IReadOnlyList<ChipDefinition>? chips = null, IReadOnlyList<MockupNote>? mockupNotes = null)
    {
        Circuit = circuit ?? new Circuit();
        MockupLayout = mockupLayout ?? new ViewLayout(RepresentationKind.Mockup);
        SchematicLayout = schematicLayout ?? new ViewLayout(RepresentationKind.Schematic);
        if (MockupLayout.Kind != RepresentationKind.Mockup || SchematicLayout.Kind != RepresentationKind.Schematic)
            throw new ArgumentException("Перепутаны раскладки представлений.");
        Breadboards = Array.AsReadOnly(breadboards?.ToArray() ?? []);
        Footprints = Array.AsReadOnly(footprints?.ToArray() ?? []);
        this.bindings = bindings?.ToList() ?? [];
        this.chips = chips?.ToList() ?? [];
        MockupNotes = Array.AsReadOnly(mockupNotes?.ToArray() ?? []);
        SetSketch(sketch ?? new());
        SetSimulationSetup(simulationSetup ?? new());
    }
    /// <summary>Подписи физического макета.</summary>
    public IReadOnlyList<MockupNote> MockupNotes { get; private set; }

    /// <summary>Заменяет подписи физического макета.</summary>
    public void SetMockupNotes(IEnumerable<MockupNote> notes) => MockupNotes = Array.AsReadOnly(notes.ToArray());

    /// <summary>Общая электрическая модель.</summary>
    public Circuit Circuit { get; private set; }
    /// <summary>Физические размещения в мм.</summary>
    public ViewLayout MockupLayout { get; private set; }
    /// <summary>Символьные размещения в логических пикселях.</summary>
    public ViewLayout SchematicLayout { get; private set; }
    /// <summary>Платы, видимые только в макете.</summary>
    public IReadOnlyList<BreadboardInstance> Breadboards { get; private set; }
    /// <summary>Физические определения, пригодные для сериализации проекта.</summary>
    public IReadOnlyList<FootprintDefinition> Footprints { get; private set; }

    /// <summary>Разрешает физический корпус конкретного экземпляра с учётом его конфигурации.</summary>
    public FootprintDefinition? FootprintFor(Component component) =>
        SplitterGeometry.Apply(Footprints.FirstOrDefault(footprint => footprint.ComponentDefinitionId == component.Definition.Id),
            component.SplitterOutputs);

    /// <summary>Изменяет число выходов разветвителя, сохраняя все подключённые провода.</summary>
    public string? ResizeSplitter(Guid id, int outputs)
    {
        var component = Circuit.Components.FirstOrDefault(part => part.Id == id);
        if (component?.SplitterOutputs is not int current || outputs is < SplitterGeometry.MinimumOutputs or > SplitterGeometry.MaximumOutputs ||
            FootprintFor(component) is not { } footprint)
            return "Выберите допустимое число выходов разветвителя.";
        if (outputs < current)
        {
            var removed = footprint.Contacts.Skip(outputs + 1).Select(port => port.Id).ToHashSet();
            bool Occupied(Wiring.WireEndpoint endpoint) => endpoint.Kind == Wiring.WireEndpointKind.ComponentContact &&
                endpoint.OwnerId == id && removed.Contains(endpoint.TerminalId);
            if (Circuit.Wires.Any(wire => Occupied(wire.Start) || Occupied(wire.End)))
                return "Сначала отсоедините провода от гнёзд, которые хотите убрать.";
        }
        component.SplitterOutputs = outputs;
        return null;
    }

    /// <summary>Версии пользовательских микросхем, которыми владеет проект.</summary>
    public IReadOnlyList<ChipDefinition> Chips => chips.AsReadOnly();

    /// <summary>Добавляет новую версию и корпус для размещения её экземпляров.</summary>
    public void RegisterChip(ChipDefinition definition)
    {
        if (chips.Any(chip => chip.ComponentId == definition.ComponentId)) return;
        var footprint = ChipPackage.Footprint(definition);
        chips.Add(definition);
        Footprints = Array.AsReadOnly(Footprints.Append(footprint).ToArray());
    }

    /// <summary>Находит определение установленной версии по ключу электрической модели.</summary>
    public ChipDefinition? FindChip(string componentId) => chips.Find(chip => chip.ComponentId == componentId);
    /// <summary>Вставленные физические контакты.</summary>
    public IReadOnlyList<ContactBinding> Bindings => bindings.AsReadOnly();
    /// <summary>Снимок активного сеанса, исключённый из файлов проекта и истории.</summary>
    [JsonIgnore]
    public SimulationState Simulation { get; private set; } = new();
    /// <summary>Публикует снимок без изменения данных электрической схемы.</summary>
    public void SetSimulation(SimulationState state) => Simulation = state;
    /// <summary>Настройки будущего запуска и положения приборов, сохраняемые в .circ.</summary>
    public SimulationSetup SimulationSetup { get; private set; } = new();
    /// <summary>Заменяет проверенные настройки, не меняя электрическую схему.</summary>
    public void SetSimulationSetup(SimulationSetup setup)
    {
        if (setup?.IsValid != true) throw new ArgumentException("Некорректные данные симуляции.");
        SimulationSetup = setup;
    }
    /// <summary>Код проекта с независимой историей редактирования.</summary>
    public Code.SketchDocument Sketch { get; private set; } = new();
    /// <summary>Заменяет проверенные данные скетча.</summary>
    public void SetSketch(Code.SketchDocument sketch)
    {
        if (!sketch.IsValid) throw new ArgumentException("Некорректные данные скетча.");
        Sketch = sketch;
    }
    /// <summary>Создаёт экземпляр и независимые размещения.</summary>
    public Component AddComponent(ComponentDefinition definition)
    {
        var component = Circuit.AddComponent(definition);
        MockupLayout.Set(component.Id, new ComponentPlacement());
        SchematicLayout.Set(component.Id, new ComponentPlacement());
        return component;
    }
    /// <summary>Удаляет экземпляр, его размещения и вставленные контакты.</summary>
    public void RemoveComponent(Guid id)
    {
        Circuit.RemoveComponent(id); MockupLayout.Remove(id); SchematicLayout.Remove(id); Detach(id);
    }
    /// <summary>Отсоединяет компонент от отверстий без изменения схемы.</summary>
    public void Detach(Guid id) => bindings.RemoveAll(binding => binding.ComponentId == id);
    /// <summary>Заменяет проверенное состояние; определения библиотеки остаются общими.</summary>
    public void Restore(CircuitProject state)
    {
        Circuit = state.Circuit; MockupLayout = state.MockupLayout; SchematicLayout = state.SchematicLayout;
        Breadboards = state.Breadboards; Footprints = state.Footprints;
        MockupNotes = state.MockupNotes;
        chips.Clear(); chips.AddRange(state.Chips);
        bindings.Clear(); bindings.AddRange(state.Bindings);
        Simulation = new();
    }
    /// <summary>Перемещает/поворачивает плату вместе со вставленными экземплярами и внутренними изгибами.</summary>
    public void TransformBoard(Guid id,double x,double y,int rotation)
    {
        if(!double.IsFinite(x)||!double.IsFinite(y)||rotation is not (0 or 90 or 180 or 270))throw new ArgumentException("Некорректное размещение breadboard.");
        var old=Breadboards.Single(b=>b.Id==id);var next=old with {X=x,Y=y,Rotation=rotation};
        var mounted=Bindings.Where(b=>b.BoardId==id).Select(b=>b.ComponentId).ToHashSet();
        Layout.LayoutPoint Transform(Layout.LayoutPoint p)=>BreadboardGeometry.World(next,BreadboardGeometry.Local(old,p));
        foreach(var component in mounted)
        {
            var pose=MockupLayout.Get(component);var p=Transform(new(pose.X,pose.Y));
            MockupLayout.Set(component,new(p.X,p.Y,(pose.Rotation+rotation-old.Rotation+360)%360,pose.SpanHoles));
        }
        bool Attached(Wiring.WireEndpoint e)=>e.Kind==Wiring.WireEndpointKind.BreadboardHole ? e.OwnerId==id : mounted.Contains(e.OwnerId);
        foreach(var wire in Circuit.Wires.Where(w=>Attached(w.Start)&&Attached(w.End)).ToArray())
            Circuit.PutWire(new Wire(wire.Id,wire.Start,wire.End,wire.Color,wire.ControlPoints.Select(Transform).ToArray(), wire.IsStraight));
        Breadboards=Array.AsReadOnly(Breadboards.Select(b=>b.Id==id?next:b).ToArray());
    }
    internal void CommitInsertion(Guid id, ComponentPlacement placement, IReadOnlyList<ContactBinding> newBindings)
    {
        MockupLayout.Set(id, placement);
        Detach(id); bindings.AddRange(newBindings);
    }
}
