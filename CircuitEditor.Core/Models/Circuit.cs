using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Models;

/// <summary>Электрическая схема, независимая от физических и символьных размещений.</summary>
public sealed class Circuit
{
    private readonly List<Component> components;
    private readonly List<Wire> wires;
    private readonly Dictionary<string, int> counters;
    /// <summary>Создаёт или восстанавливает схему с устойчивыми ID и счётчиками.</summary>
    [JsonConstructor]
    public Circuit(IReadOnlyList<Component>? components = null, IReadOnlyList<Wire>? wires = null,
        IReadOnlyDictionary<string, int>? counters = null)
    {
        this.components = components?.ToList() ?? [];
        this.wires = wires?.ToList() ?? [];
        this.counters = counters?.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal) ?? [];
        if (this.components.Select(item => item.Id).Distinct().Count() != this.components.Count
            || this.components.Select(item => item.Reference).Distinct().Count() != this.components.Count)
            throw new ArgumentException("Повторяющиеся компоненты.");
        foreach (var component in this.components)
        {
            var prefix = component.Definition.Prefix;
            if (component.Reference.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(component.Reference.AsSpan(prefix.Length), out var number))
                this.counters[prefix] = Math.Max(this.counters.GetValueOrDefault(prefix), number);
        }
    }
    /// <summary>Электрические экземпляры.</summary>
    public IReadOnlyList<Component> Components => components.AsReadOnly();
    /// <summary>Существующие соединения; создание перемычек не входит в этап 3.</summary>
    public IReadOnlyList<Wire> Wires => wires.AsReadOnly();
    /// <summary>Сериализуемые счётчики, включая удалённые обозначения.</summary>
    public IReadOnlyDictionary<string, int> Counters => counters;
    /// <summary>Добавляет экземпляр с монотонным обозначением.</summary>
    public Component AddComponent(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var next = counters.GetValueOrDefault(definition.Prefix) + 1;
        var component = new Component(definition, $"{definition.Prefix}{next}");
        AssignAvailableI2cAddress(component);
        counters[definition.Prefix] = next; components.Add(component);
        return component;
    }

    internal void AssignAvailableI2cAddress(Component component, IEnumerable<Component>? additional = null)
    {
        if (component.I2cAddress is not { } preferred) return;
        const int pcf8574Base = 0x20;
        const int pcf8574aBase = 0x38;
        const int addressesPerVariant = 8;
        const int alternateSsd1306 = 0x3D;
        var used = components.Concat(additional ?? []).Select(part => part.I2cAddress)
            .OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(preferred)) return;
        // New devices are unwired, so reserve an address across the project. Existing addresses are retained.
        IEnumerable<int> hardware = component.Definition.Id switch
        {
            "lcd1602-i2c" => Enumerable.Range(pcf8574Base, addressesPerVariant).Reverse()
                .Concat(Enumerable.Range(pcf8574aBase, addressesPerVariant).Reverse()),
            "oled-ssd1306" => [alternateSsd1306],
            _ => []
        };
        var virtualRange = Enumerable.Range(ComponentProperties.MinimumI2cAddress,
            ComponentProperties.MaximumI2cAddress - ComponentProperties.MinimumI2cAddress + 1);
        var address = hardware.Concat(virtualRange).Select(value => $"0x{value:X2}")
            .FirstOrDefault(value => !used.Contains(value));
        if (address is not null) component.TryUpdateProperties(component.Value, component.Color, address, out _);
    }
    /// <summary>Удаляет экземпляр и инцидентные соединения.</summary>
    public bool RemoveComponent(Guid id)
    {
        var component = components.Find(item => item.Id == id);
        if (component is null) return false;
        wires.RemoveAll(wire => wire.Start.ComponentId == id || wire.End.ComponentId == id);
        return components.Remove(component);
    }
    /// <summary>Проверяет ссылки существующей модели Wire.</summary>
    public bool TryAddWire(Wire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        if (!ContainsPin(wire.Start) || !ContainsPin(wire.End) ||
            wires.Any(item => item.Start == wire.Start && item.End == wire.End || item.Start == wire.End && item.End == wire.Start))
            return false;
        wires.Add(wire); return true;
    }
    /// <summary>Меняет проверенную физическую перемычку, сохраняя порядок.</summary>
    public void PutWire(Wire wire)
    {
        var index = wires.FindIndex(w => w.Id == wire.Id);
        if (index < 0) wires.Add(wire); else wires[index] = wire;
    }
    /// <summary>Удаляет перемычку.</summary>
    public bool RemoveWire(Guid id) => wires.RemoveAll(w => w.Id == id) > 0;
    private bool ContainsPin(CircuitEditor.Core.Wiring.WireEndpoint reference) => components.Any(component => component.Id == reference.ComponentId
        && component.Definition.Pins.Any(pin => pin.Id == reference.TerminalId && pin.IsExposed));
}
