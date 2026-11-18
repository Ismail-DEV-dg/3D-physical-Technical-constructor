using CircuitEditor.Core.Resistance;

namespace CircuitEditor.Core.Physical;

/// <summary>Точная четырёх- или пятиполосная маркировка процедурного резистора.</summary>
public static class ResistorMarking
{
    private static readonly string[] Digits = ["#171717", "#704020", "#D32F2F", "#F27822", "#F7D12E",
        "#238B45", "#2466D6", "#843DB0", "#7D8790", "#F2F2EC"];
    private const string Gold = "#C9A438";
    private const string Silver = "#B8BDC7";
    /// <summary>Возвращает полосы без округления; для трёх значащих цифр использует пять полос.</summary>
    public static IReadOnlyList<string> GetBands(string value, double tolerancePercent = 5)
    {
        if (!ResistanceFormatter.TryParse(value, out var number)) return [];
        foreach (var significantDigits in new[] { 2, 3 })
        {
            var exponent = (int)Math.Floor(Math.Log10((double)number)) - significantDigits + 1;
            if (exponent is < -2 or > 9) continue;
            var scale = (decimal)Math.Pow(10, exponent);
            var digits = number / scale;
            if (digits != decimal.Truncate(digits)) continue;
            var code = (int)digits;
            var bands = new List<string>();
            if (significantDigits == 3) { bands.Add(Digits[code / 100]); code %= 100; }
            bands.Add(Digits[code / 10]); bands.Add(Digits[code % 10]);
            bands.Add(exponent == -1 ? Gold : exponent == -2 ? Silver : Digits[exponent]);
            bands.Add(tolerancePercent == 10 ? Silver : Gold);
            return bands;
        }
        return [];
    }
}
