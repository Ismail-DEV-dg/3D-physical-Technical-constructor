namespace CircuitEditor.Core.Models;

/// <summary>Неизменяемая запись JSON-библиотеки, общая для экземпляров компонента.</summary>
public sealed class ComponentDefinition
{
    /// <summary>Создаёт проверенное библиотечное описание.</summary>
    public ComponentDefinition(string id, string name, string prefix, string category, string symbol,
        string description, double voltage, IReadOnlyList<Pin> pins,
        string defaultValue = "", string defaultColor = "#00E5FF", string? defaultI2cAddress = null)
    {
        foreach (var text in new[] { id, name, prefix, category, symbol, description })
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultValue);
        if (!prefix.All(char.IsAsciiLetter) || !double.IsFinite(voltage) || voltage <= 0)
            throw new ArgumentException("Некорректные параметры библиотечного компонента.");
        ArgumentNullException.ThrowIfNull(pins);
        if (pins.Count == 0 || pins.Any(pin => pin is null)
            || pins.Select(pin => pin.Id).Distinct(StringComparer.Ordinal).Count() != pins.Count)
            throw new ArgumentException("Выводы отсутствуют или их идентификаторы повторяются.");
        if (!ComponentProperties.IsColor(defaultColor) || !ComponentProperties.IsI2cAddress(defaultI2cAddress))
            throw new ArgumentException("Некорректный цвет или I2C-адрес.");
        Id = id; Name = name; Prefix = prefix; Category = category; Symbol = symbol;
        Description = description; Voltage = voltage;
        Pins = Array.AsReadOnly(pins.ToArray()); DefaultValue = defaultValue;
        DefaultColor = defaultColor; DefaultI2cAddress = defaultI2cAddress;
    }

    /// <summary>Стабильный идентификатор типа.</summary>
    public string Id { get; }
    /// <summary>Название.</summary>
    public string Name { get; }
    /// <summary>Префикс автонумерации.</summary>
    public string Prefix { get; }
    /// <summary>Категория палитры.</summary>
    public string Category { get; }
    /// <summary>Идентификатор векторного символа.</summary>
    public string Symbol { get; }
    /// <summary>Описание и особенности.</summary>
    public string Description { get; }
    /// <summary>Номинальное рабочее напряжение в вольтах; не абсолютный максимум.</summary>
    public double Voltage { get; }
    /// <summary>Выводы библиотечного типа.</summary>
    public IReadOnlyList<Pin> Pins { get; }
    /// <summary>Начальный номинал экземпляра.</summary>
    public string DefaultValue { get; }
    /// <summary>Начальный цвет в формате #RRGGBB.</summary>
    public string DefaultColor { get; }
    /// <summary>Начальный семибитный I2C-адрес в hex либо null.</summary>
    public string? DefaultI2cAddress { get; }
}
