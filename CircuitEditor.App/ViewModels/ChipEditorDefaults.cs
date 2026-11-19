using System.Globalization;
using CircuitEditor.Core.Chips;

namespace CircuitEditor.App.ViewModels;

internal static class ChipEditorDefaults
{
    public static ChipDefinition Create(string family) => new(Guid.NewGuid(), 1, "Логическая микросхема", "",
        "DIP-8", family,
        [new(1,"A",ChipPinType.Input), new(2,"B",ChipPinType.Input), new(3,"Y",ChipPinType.Output),
         new(4,"GND",ChipPinType.Gnd), Unused(5), Unused(6), Unused(7), new(8,"VCC",ChipPinType.Vcc)],
        new ChipTruthTable([new([1,2], [3], [["00","1"], ["01","1"], ["10","1"], ["11","0"]])]));

    public static ChipPin Unused(int number) =>
        new(number, "NC" + number.ToString(CultureInfo.InvariantCulture), ChipPinType.Input, IsUnused: true);
}
