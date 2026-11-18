using System.Globalization;
using System.Text.RegularExpressions;

namespace CircuitEditor.Core.Resistance;

/// <summary>Точное форматирование и проверка сопротивления с единицей измерения.</summary>
public static class ResistanceFormatter
{
    private const decimal Kilo = 1_000m;
    private const decimal Mega = 1_000_000m;
    /// <summary>Форматирует без округления, используя Ω, kΩ или MΩ.</summary>
    public static string Format(decimal ohms)
    {
        var divisor = ohms >= Mega ? Mega : ohms >= Kilo ? Kilo : 1m;
        var unit = divisor == Mega ? "MΩ" : divisor == Kilo ? "kΩ" : "Ω";
        return (ohms / divisor).ToString("0.############################", CultureInfo.InvariantCulture) + " " + unit;
    }

    /// <summary>Принимает число с точкой/запятой и единицей Ω/Ом, kΩ/кОм, MΩ/МОм в диапазоне редактора.</summary>
    public static bool TryParse(string? text, out decimal ohms)
    {
        ohms = 0;
        if (text is null || text.Length > 80) return false;
        var match = Regex.Match(text.Trim(), @"^(?<number>\d+(?:[.,]\d+)?)\s*(?<unit>Ω|Ω|Ом|ohm|[kKкК](?:Ω|Ω|Ом|ohm)|[MМ](?:Ω|Ω|Ом|ohm))$",
            RegexOptions.CultureInvariant);
        if (!match.Success || !decimal.TryParse(match.Groups["number"].Value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)) return false;
        var unit = match.Groups["unit"].Value;
        var multiplier = unit[0] is 'M' or 'М' ? Mega : unit[0] is 'k' or 'K' or 'к' or 'К' ? Kilo : 1m;
        if (number < ResistorSeries.MinimumOhms / multiplier || number > ResistorSeries.MaximumOhms / multiplier) return false;
        ohms = number * multiplier;
        return true;
    }
}
