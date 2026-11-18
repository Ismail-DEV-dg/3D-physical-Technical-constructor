namespace CircuitEditor.Core.Resistance;

/// <summary>Ряды IEC 60063; таблица: https://www.vishay.com/docs/31001/dectable.pdf.</summary>
public static class ResistorSeries
{
    /// <summary>Минимальный номинал редактора, Ом.</summary>
    public const decimal MinimumOhms = 1m;
    /// <summary>Максимальный номинал редактора, Ом.</summary>
    public const decimal MaximumOhms = 10_000_000m;
    private static readonly int[] E12 = [10,12,15,18,22,27,33,39,47,56,68,82];
    private static readonly int[] E24 = [10,11,12,13,15,16,18,20,22,24,27,30,33,36,39,43,47,51,56,62,68,75,82,91];
    private static readonly int[] E96 = [100,102,105,107,110,113,115,118,121,124,127,130,133,137,140,143,
        147,150,154,158,162,165,169,174,178,182,187,191,196,200,205,210,
        215,221,226,232,237,243,249,255,261,267,274,280,287,294,301,309,
        316,324,332,340,348,357,365,374,383,392,402,412,422,432,442,453,
        464,475,487,499,511,523,536,549,562,576,590,604,619,634,649,665,
        681,698,715,732,750,768,787,806,825,845,866,887,909,931,953,976];

    /// <summary>Возвращает упорядоченные номиналы включительно от 1 Ом до 10 МОм.</summary>
    public static IReadOnlyList<decimal> GetValues(ResistorSeriesKind series)
    {
        var mantissas = series switch
        {
            ResistorSeriesKind.E12 => E12,
            ResistorSeriesKind.E24 => E24,
            ResistorSeriesKind.E96 => E96,
            _ => throw new ArgumentOutOfRangeException(nameof(series))
        };
        var divisor = series == ResistorSeriesKind.E96 ? 100m : 10m;
        var result = new List<decimal>();
        for (var decade = MinimumOhms; decade <= MaximumOhms; decade *= 10)
            foreach (var mantissa in mantissas)
            {
                var value = mantissa / divisor * decade;
                if (value <= MaximumOhms) result.Add(value);
            }
        return result.AsReadOnly();
    }
}
