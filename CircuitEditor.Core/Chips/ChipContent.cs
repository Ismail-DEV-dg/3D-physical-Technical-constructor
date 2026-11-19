using System.Text.Json;

namespace CircuitEditor.Core.Chips;

/// <summary>Сравнивает сохранённые данные даже после десериализации коллекций.</summary>
public static class ChipContent
{
    /// <summary>Проверяет совпадение метаданных, распиновки и поведения.</summary>
    public static bool Matches(ChipDefinition left, ChipDefinition right) =>
        left.Id == right.Id && left.DefinitionVersion == right.DefinitionVersion &&
        JsonSerializer.Serialize(left with { Pins = left.Pins.OrderBy(pin => pin.Number).ToArray() }) ==
        JsonSerializer.Serialize(right with { Pins = right.Pins.OrderBy(pin => pin.Number).ToArray() });
}
