using System.Text.Json.Serialization;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Core.Models;

/// <summary>Одна электрическая модель, две раскладки, корпуса и привязки breadboard.</summary>
public sealed class CircuitProject
{
    private readonly List<ContactBinding> bindings;
    /// <summary>Создаёт или восстанавливает полный проект без файлового ввода-вывода.</summary>
    [JsonConstructor]
    public CircuitProject(Circuit? circuit = null, ViewLayout? mockupLayout = null, ViewLayout? schematicLayout = null,
        IReadOnlyList<BreadboardInstance>? breadboards = null, IReadOnlyList<FootprintDefinition>? footprints = null,
        IReadOnlyList<ContactBinding>? bindings = null, SimulationState? simulation = null)
    {
        Circuit = circuit ?? new Circuit();
        MockupLayout = mockupLayout ?? new ViewLayout(RepresentationKind.Mockup);
        SchematicLayout = schematicLayout ?? new ViewLayout(RepresentationKind.Schematic);
        if (MockupLayout.Kind != RepresentationKind.Mockup || SchematicLayout.Kind != RepresentationKind.Schematic)
            throw new ArgumentException("Перепутаны раскладки представлений.");
        Breadboards = Array.AsReadOnly(breadboards?.ToArray() ?? []);
        Footprints = Array.AsReadOnly(footprints?.ToArray() ?? []);
        this.bindings = bindings?.ToList() ?? [];
        Simulation = simulation ?? new SimulationState();
    }
    /// <summary>Общая электрическая модель.</summary>
    public Circuit Circuit { get; private set; }
    /// <summary>Физические размещения в мм.</summary>
    public ViewLayout MockupLayout { get; private set; }
    /// <summary>Символьные размещения в логических пикселях.</summary>
    public ViewLayout SchematicLayout { get; private set; }
    /// <summary>Платы, видимые только в макете.</summary>
    public IReadOnlyList<BreadboardInstance> Breadboards { get; private set; }
    /// <summary>Физические определения, пригодные для сериализации проекта.</summary>
    public IReadOnlyList<FootprintDefinition> Footprints { get; }
    /// <summary>Вставленные физические контакты.</summary>
    public IReadOnlyList<ContactBinding> Bindings => bindings.AsReadOnly();
    /// <summary>Пока пустое состояние симуляции.</summary>
    public SimulationState Simulation { get; }
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
        Breadboards = state.Breadboards; bindings.Clear(); bindings.AddRange(state.Bindings);
    }
    internal void CommitInsertion(Guid id, ComponentPlacement placement, IReadOnlyList<ContactBinding> newBindings)
    {
        MockupLayout.Set(id, placement);
        Detach(id); bindings.AddRange(newBindings);
    }
}
