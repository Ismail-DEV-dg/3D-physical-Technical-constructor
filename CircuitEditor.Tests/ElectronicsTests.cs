using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Resistance;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class ElectronicsTests
{
    [Theory]
    [InlineData("UI")][InlineData("UR")][InlineData("UP")][InlineData("IR")][InlineData("IP")][InlineData("RP")]
    public void AnyTwoOhmQuantitiesDetermineSameResult(string pair)
    {
        var r=OhmCalculator.Solve(pair.Contains('U')?10:null,pair.Contains('I')?.1:null,pair.Contains('R')?100:null,pair.Contains('P')?1:null);
        Assert.Null(r.Error);Assert.Equal(10,r.Value!.Voltage,8);Assert.Equal(.1,r.Value.Current,8);Assert.Equal(100,r.Value.Resistance,8);Assert.Equal(1,r.Value.Power,8);
    }
    [Fact]
    public void BadInputIsReportedAndUnitsAreParsed()
    {
        Assert.NotNull(OhmCalculator.Solve(u:0,p:1).Error);Assert.NotNull(OhmCalculator.Solve(r:0,u:5).Error);
        Assert.True(QuantityFormatter.TryParse("20 мА","I",out var current));Assert.Equal(.02,current);
        Assert.True(QuantityFormatter.TryParse("500 мВ","U",out var voltage));Assert.Equal(.5,voltage);
        Assert.False(QuantityFormatter.TryParse("20 В","I",out _));Assert.Equal(2.5,VoltageDividerCalculator.Calculate(5,1000,1000));
    }
    [Theory]
    [InlineData(ResistorSeriesKind.E12)][InlineData(ResistorSeriesKind.E24)][InlineData(ResistorSeriesKind.E96)]
    public void LedSelectionRoundsUpAndHonorsColorData(ResistorSeriesKind series)
    {
        var profile=new JsonElectricalLibraryRepository().Load().Value!.LedProfiles.First(p=>p.Id=="blue");
        var r=LedResistorCalculator.Calculate(5,.015,profile,series);Assert.Null(r.Error);
        Assert.True(r.Value!.Resistance>=(5-profile.Vf)/.015);Assert.True(r.Value.Current<=.015);
        Assert.Equal(profile.Vf,r.Value.Vf);
    }
    [Theory]
    [InlineData("220 Ω",4)][InlineData("1.21 kΩ",5)][InlineData("1 Ω",5)]
    public void BandsRoundTrip(string text,int count)
    {
        var bands=ResistorMarking.GetBands(text,1,count);Assert.Equal(count,bands.Count);
        var value=ResistorMarking.Decode(bands);Assert.Null(value.Error);Assert.True(ResistanceFormatter.TryParse(text,out var expected));
        Assert.Equal(expected,value.Value!.Ohms);Assert.Equal(1,value.Value.TolerancePercent);
    }
}
