namespace CircuitEditor.Core.Chips;

/// <summary>Проверяет метаданные, назначение выводов и корректность поведения.</summary>
public static class ChipValidator
{
    /// <summary>Максимальная задержка распространения в микросекундах модельного времени.</summary>
    public const long MaximumDelayUs = 5_000;

    /// <summary>Возвращает ошибки определения, включая ошибки привязки его поведения.</summary>
    public static IReadOnlyList<string> Validate(ChipDefinition chip,
        IReadOnlyDictionary<string, LogicFamilyProfile> families)
    {
        List<string> errors = [];
        if (chip.Id == Guid.Empty || chip.DefinitionVersion < 1 || string.IsNullOrWhiteSpace(chip.Name))
            errors.Add("Укажите имя микросхемы и корректную версию.");
        if (string.IsNullOrWhiteSpace(chip.Family) || !families.ContainsKey(chip.Family))
            errors.Add("Укажите известное логическое семейство.");
        if (chip.PropagationDelayUs is < 0 or > MaximumDelayUs)
            errors.Add($"Допустимая задержка: 0–{MaximumDelayUs} мкс.");
        var count = ChipPackage.PinCount(chip.Package);
        if (count == 0 || chip.Pins is null || chip.Pins.Count != count ||
            chip.Pins.Any(pin => pin is null) ||
            !chip.Pins.Select(pin => pin.Number).Order().SequenceEqual(Enumerable.Range(1, count)))
        {
            errors.Add("Номера выводов должны последовательно заполнять выбранный корпус DIP.");
            return errors;
        }

        if (chip.Pins.Any(pin => string.IsNullOrWhiteSpace(pin.Name) || pin.Name.Contains('\r')
                || pin.Name.Contains('\n') || !Enum.IsDefined(pin.Type)
                || pin.IsUnused && pin.Type != ChipPinType.Input)
            || chip.Pins.Select(pin => pin.Name?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != count)
            errors.Add("Укажите уникальное имя и корректный тип каждого вывода.");
        if (!chip.Pins.Any(pin => pin.Type == ChipPinType.Vcc) || !chip.Pins.Any(pin => pin.Type == ChipPinType.Gnd))
            errors.Add("Добавьте выводы питания VCC и GND.");

        var inputs = chip.Pins.Where(pin => pin.Reads).Select(pin => pin.Number).ToHashSet();
        var outputs = chip.Pins.Where(pin => pin.Drives).Select(pin => pin.Number).ToHashSet();
        foreach (var pin in chip.Pins.Where(pin => pin.Type == ChipPinType.Bidirectional && !pin.EnablePin.HasValue))
            errors.Add($"Двунаправленный вывод {pin.Number}: укажите вход разрешения драйвера.");
        foreach (var pin in chip.Pins.Where(pin => pin.EnablePin.HasValue))
        {
            if (!pin.Drives || pin.EnablePin == pin.Number || !inputs.Contains(pin.EnablePin!.Value))
                errors.Add($"Вывод {pin.Number}: разрешение выхода должно ссылаться на отдельный вход.");
        }

        if (outputs.Count == 0) errors.Add("Добавьте хотя бы один выход.");
        var enablePins = chip.Pins.Where(pin => pin.EnablePin.HasValue).Select(pin => pin.EnablePin!.Value);
        switch (chip.Behavior)
        {
            case ChipTruthTable table:
                ChipTruthLogic.Validate(table, inputs, outputs, enablePins, errors);
                break;
            case ChipGraph graph:
                ChipGraphValidation.Validate(graph, inputs, outputs, enablePins, errors);
                break;
            default:
                errors.Add("Укажите поддерживаемое поведение микросхемы.");
                break;
        }

        return errors;
    }
}
