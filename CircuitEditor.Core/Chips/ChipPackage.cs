using System.Globalization;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Core.Chips;

/// <summary>Геометрия стандартного узкого DIP с шагом 2,54 мм и межрядным расстоянием 7,62 мм.</summary>
public static class ChipPackage
{
    private const double RowSeparation = 3 * PhysicalSettings.FreeGridMm;
    private const double BodyInset = PhysicalSettings.FreeGridMm / 3;
    private const double EndMargin = PhysicalSettings.FreeGridMm / 2;

    /// <summary>Поддерживаемые корпуса.</summary>
    public static IReadOnlyList<string> Supported { get; } = Array.AsReadOnly(new[] { "DIP-8", "DIP-14", "DIP-16", "DIP-20" });

    /// <summary>Число выводов выбранного корпуса или ноль для неизвестного корпуса.</summary>
    public static int PinCount(string package) => package switch
    {
        "DIP-8" => 8, "DIP-14" => 14, "DIP-16" => 16, "DIP-20" => 20, _ => 0
    };

    /// <summary>Стабильный идентификатор электрического и физического вывода.</summary>
    public static string PinId(int number) => number.ToString(CultureInfo.InvariantCulture);

    /// <summary>Строит корпус с нумерацией против часовой стрелки от ключа.</summary>
    public static FootprintDefinition Footprint(ChipDefinition chip)
    {
        var count = PinCount(chip.Package);
        if (count == 0) throw new ArgumentException("Неизвестный корпус DIP.", nameof(chip));
        var side = count / 2;
        var contacts = Enumerable.Range(1, count).Select(pin =>
        {
            var left = pin <= side;
            var row = left ? pin - 1 : count - pin;
            return new ContactDefinition(PinId(pin), PinId(pin), left ? 0 : RowSeparation,
                row * PhysicalSettings.FreeGridMm);
        }).ToArray();
        return new(chip.ComponentId, chip.ComponentId, "dip-chip", "breadboard",
            new(BodyInset, -EndMargin, RowSeparation - 2 * BodyInset, side * PhysicalSettings.FreeGridMm),
            contacts, [0, 90, 180, 270]);
    }

    /// <summary>Строит электрическое описание для палитры и условного символа.</summary>
    public static ComponentDefinition Component(ChipDefinition chip, LogicFamilyProfile family) =>
        new(chip.ComponentId, chip.Name, "U", "Микросхемы", "chip",
            string.IsNullOrWhiteSpace(chip.Description) ? chip.Name : chip.Description, family.NominalSupply,
            chip.Pins.OrderBy(pin => pin.Number).Select(pin => new Pin(PinId(pin.Number), pin.Name,
                pin.Type switch { ChipPinType.Vcc => PinType.Power, ChipPinType.Gnd => PinType.Ground, _ => PinType.DigitalIO },
                isInputOnly: pin.Type == ChipPinType.Input, isNoConnect: pin.IsUnused)).ToArray(), chip.Package);
}
