using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Inputs;

/// <summary>Неизменяемый JSON-снимок условий конкретного экземпляра.</summary>
public sealed class ComponentInputs
{
    /// <summary>Копирует данные; изменения исходного словаря не меняют модель.</summary>
    [JsonConstructor]
    public ComponentInputs(IReadOnlyDictionary<ComponentInput, double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Any(pair => !Enum.IsDefined(pair.Key) || !double.IsFinite(pair.Value)))
            throw new ArgumentException("Некорректные входные условия.");
        Values = new ReadOnlyDictionary<ComponentInput, double>(values.ToDictionary(pair => pair.Key, pair => pair.Value));
    }
    /// <summary>Значения, ключи которых определяются типом компонента.</summary>
    public IReadOnlyDictionary<ComponentInput, double> Values { get; }
}
