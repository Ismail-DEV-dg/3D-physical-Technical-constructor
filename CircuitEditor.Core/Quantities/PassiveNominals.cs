using System.Globalization;
using System.Text.RegularExpressions;
using CircuitEditor.Core.Resistance;

namespace CircuitEditor.Core.Quantities;

/// <summary>Ряд E12, единицы и проверка номиналов конденсаторов и катушек.</summary>
public static class PassiveNominals
{
    /// <summary>Минимальный номинал в базовых единицах.</summary>
    public const decimal Minimum = 1;
    /// <summary>Верхняя граница ёмкости, 1000 мкФ в пФ.</summary>
    public const decimal MaximumPicofarads = 1_000_000_000;
    /// <summary>Верхняя граница индуктивности, 100 мГн в мкГн.</summary>
    public const decimal MaximumMicrohenries = 100_000;
    private const decimal Kilo = 1000;
    private const decimal Mega = 1_000_000;

    /// <summary>Возвращает возрастающий ряд E12 с включёнными границами диапазона.</summary>
    public static IReadOnlyList<decimal> GetValues(PassiveQuantity quantity)
    {
        var maximum = Maximum(quantity);
        var mantissas = ResistorSeries.GetValues(ResistorSeriesKind.E12).Take(12).ToArray();
        var result = new List<decimal>();
        for (var decade = Minimum; decade <= maximum; decade *= 10)
            foreach (var mantissa in mantissas)
                if (mantissa * decade <= maximum) result.Add(mantissa * decade);
        return result.AsReadOnly();
    }

    /// <summary>Форматирует пФ/нФ/мкФ либо мкГн/мГн без округления.</summary>
    public static string Format(PassiveQuantity quantity, decimal value)
    {
        var capacitance = quantity == PassiveQuantity.Capacitance;
        var divisor = capacitance && value >= Mega ? Mega : value >= Kilo ? Kilo : 1;
        var unit = capacitance ? divisor == Mega ? "μF" : divisor == Kilo ? "nF" : "pF"
            : divisor == Kilo ? "mH" : "μH";
        return (value / divisor).ToString("0.############################", CultureInfo.InvariantCulture) + " " + unit;
    }

    /// <summary>Проверяет число с обязательной единицей; понимает русские и SI-обозначения.</summary>
    public static bool TryParse(PassiveQuantity quantity, string? text, out decimal value)
    {
        value = 0;
        if (!Enum.IsDefined(quantity) || text is null || text.Length > 80) return false;
        var match = Regex.Match(text.Trim(), @"^(?<number>\d+(?:[.,]\d+)?)\s*(?<unit>\S+)$", RegexOptions.CultureInvariant);
        if (!match.Success || !decimal.TryParse(match.Groups["number"].Value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)) return false;
        var unit = match.Groups["unit"].Value;
        var multiplier = quantity == PassiveQuantity.Capacitance ? unit switch
        { "pF" or "пФ" => 1m, "nF" or "нФ" => Kilo, "μF" or "µF" or "uF" or "мкФ" => Mega, _ => 0 }
            : unit switch { "μH" or "µH" or "uH" or "мкГн" => 1m, "mH" or "мГн" => Kilo, _ => 0 };
        if (multiplier == 0 || number < Minimum / multiplier || number > Maximum(quantity) / multiplier) return false;
        value = number * multiplier; return true;
    }

    private static decimal Maximum(PassiveQuantity quantity) => quantity switch
    {
        PassiveQuantity.Capacitance => MaximumPicofarads,
        PassiveQuantity.Inductance => MaximumMicrohenries,
        _ => throw new ArgumentOutOfRangeException(nameof(quantity))
    };
}
