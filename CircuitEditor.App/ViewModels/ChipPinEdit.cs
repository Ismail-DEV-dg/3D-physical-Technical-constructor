using CircuitEditor.Core.Chips;

namespace CircuitEditor.App.ViewModels;

public sealed class ChipPinEdit(ChipPin pin)
{
    public int Number => pin.Number;
    public string Name { get; set; } = pin.Name;
    public ChipPinType Type { get; set; } = pin.Type;
    public string Description { get; set; } = pin.Description;
    public int UnusedIndex { get; set; } = pin.IsUnused ? 1 : 0;
    public string EnablePin { get; set; } = pin.EnablePin?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
    public int EnableIndex { get; set; } = pin.EnableLow ? 1 : 0;

    public ChipPin ToPin()
    {
        int? enable = string.IsNullOrWhiteSpace(EnablePin) ? null : ChipEditorText.ParseNumber(EnablePin);
        return new(Number, Name.Trim(), Type, Description.Trim(), UnusedIndex == 1, enable, EnableIndex == 1);
    }
}
