using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Resistance;

namespace CircuitEditor.Tests;

public sealed class ResistanceTests
{
    [Theory]
    [InlineData(ResistorSeriesKind.E12, 85)]
    [InlineData(ResistorSeriesKind.E24, 169)]
    [InlineData(ResistorSeriesKind.E96, 673)]
    public void SeriesContainAllDecadesWithoutDuplicates(ResistorSeriesKind series, int count)
    {
        var values = ResistorSeries.GetValues(series);
        Assert.Equal(count, values.Count);
        Assert.Equal(1m, values[0]); Assert.Equal(10_000_000m, values[^1]);
        Assert.Equal(values.Order().Distinct(), values);
        foreach (var value in values)
        {
            var text = ResistanceFormatter.Format(value);
            Assert.True(ResistanceFormatter.TryParse(text, out var restored));
            Assert.Equal(value, restored);
            Assert.InRange(ResistorMarking.GetBands(text).Count, 4, 5);
        }
    }

    [Theory]
    [InlineData("1 Ω", 1)]
    [InlineData("2,35 kΩ", 2350)]
    [InlineData("10 МОм", 10000000)]
    [InlineData("1.02 Ом", 1.02)]
    public void CustomInputAcceptsUnitsAndBothDecimalSeparators(string text, double expected)
    {
        Assert.True(ResistanceFormatter.TryParse(text, out var actual));
        Assert.Equal((decimal)expected, actual);
    }

    [Theory]
    [InlineData("220")]
    [InlineData("0 Ω")]
    [InlineData("0.99 Ω")]
    [InlineData("10.01 MΩ")]
    [InlineData("NaN Ω")]
    [InlineData("-2 kΩ")]
    [InlineData("1e3 Ω")]
    [InlineData("1 mΩ")]
    [InlineData("99999999999999999999999999999 MΩ")]
    public void InvalidInputIsRejected(string text) => Assert.False(ResistanceFormatter.TryParse(text, out _));

    [Theory]
    [InlineData(1, "1 Ω")]
    [InlineData(999, "999 Ω")]
    [InlineData(1000, "1 kΩ")]
    [InlineData(2350, "2.35 kΩ")]
    [InlineData(1000000, "1 MΩ")]
    public void FormatUsesAppropriateUnit(double value, string expected) =>
        Assert.Equal(expected, ResistanceFormatter.Format((decimal)value));

    [Fact]
    public void ThreeSignificantDigitsUseExactFiveBands()
    {
        Assert.Equal(new[] { "#704020", "#171717", "#D32F2F", "#B8BDC7", "#C9A438" }, ResistorMarking.GetBands("1.02 Ω"));
        Assert.Empty(ResistorMarking.GetBands("1.234 Ω"));
    }
}
