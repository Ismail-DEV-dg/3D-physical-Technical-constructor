using CircuitEditor.Core.Quantities;

namespace CircuitEditor.Tests;

public sealed class PassiveNominalTests
{
    [Theory]
    [InlineData(PassiveQuantity.Capacitance, 109, 1000000000)]
    [InlineData(PassiveQuantity.Inductance, 61, 100000)]
    public void E12RangeIsOrderedUniqueAndRoundTrips(PassiveQuantity quantity, int count, long maximum)
    {
        var values = PassiveNominals.GetValues(quantity);
        Assert.Equal(count, values.Count); Assert.Equal(1m, values[0]); Assert.Equal((decimal)maximum, values[^1]);
        Assert.Equal(values.Order().Distinct(), values);
        foreach (var value in values)
        {
            Assert.True(PassiveNominals.TryParse(quantity, PassiveNominals.Format(quantity, value), out var restored));
            Assert.Equal(value, restored);
        }
    }
    [Theory]
    [InlineData(PassiveQuantity.Capacitance, "4,7 нФ", 4700)]
    [InlineData(PassiveQuantity.Capacitance, "100 μF", 100000000)]
    [InlineData(PassiveQuantity.Capacitance, "0.001 uF", 1000)]
    [InlineData(PassiveQuantity.Capacitance, "1 пФ", 1)]
    [InlineData(PassiveQuantity.Inductance, "2,2 мГн", 2200)]
    [InlineData(PassiveQuantity.Inductance, "47 µH", 47)]
    public void ParsesRussianAndSiUnits(PassiveQuantity quantity, string text, int expected)
    { Assert.True(PassiveNominals.TryParse(quantity, text, out var actual)); Assert.Equal(expected, actual); }
    [Theory]
    [InlineData(PassiveQuantity.Capacitance, "0 pF")]
    [InlineData(PassiveQuantity.Capacitance, "1001 μF")]
    [InlineData(PassiveQuantity.Capacitance, "100")]
    [InlineData(PassiveQuantity.Capacitance, "NaN nF")]
    [InlineData(PassiveQuantity.Capacitance, "10 mH")]
    [InlineData(PassiveQuantity.Inductance, "101 mH")]
    [InlineData(PassiveQuantity.Inductance, "-1 μH")]
    [InlineData(PassiveQuantity.Inductance, "1e3 μH")]
    public void RejectsInvalidValueWithoutOverflow(PassiveQuantity quantity, string text) =>
        Assert.False(PassiveNominals.TryParse(quantity, text, out _));
}
