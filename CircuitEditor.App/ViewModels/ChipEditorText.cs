using System.Globalization;
using CircuitEditor.Core.Chips;

namespace CircuitEditor.App.ViewModels;

internal static class ChipEditorText
{
    public static int ParseNumber(string value) =>
        int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var result) && result > 0
            ? result : throw new FormatException($"«{value}» должно быть положительным номером.");

    public static int[] Numbers(string text) =>
        text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseNumber).Distinct().Order().ToArray();

    public static string Signal(ChipSignal? wire) => wire switch
    {
        { Pin: int pin } => $"P{pin}",
        { Gate: int gate } => $"B{gate}",
        { Constant: ChipLevel level } => ChipTruthLogic.Symbol(level).ToString(),
        _ => ""
    };

    public static ChipSignal ParseSignal(string value)
    {
        var text = value.Trim().ToUpperInvariant();
        return text switch
        {
            "0" => new(Constant: ChipLevel.Low), "1" => new(Constant: ChipLevel.High),
            "X" => new(Constant: ChipLevel.X), "Z" => new(Constant: ChipLevel.Z),
            _ when text.StartsWith('P') => new(Pin: ParseNumber(text[1..])),
            _ when text.StartsWith('B') => new(Gate: ParseNumber(text[1..])),
            _ => throw new FormatException($"Источник «{value}»: укажите P2 (вывод), B1 (блок), 0, 1, X или Z.")
        };
    }
}
