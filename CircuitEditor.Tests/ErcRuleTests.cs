using CircuitEditor.Core.Validation;
namespace CircuitEditor.Tests;
public sealed class ErcRuleTests
{
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void SupplyShort(bool shorted)
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");if(shorted)e.Wire(uno,"5V",uno,"GND");
        Assert.Equal(shorted,e.Check(new SupplyShortRule()).Count>0);
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void DifferentSourcesOnSameBoard(bool joined)
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");if(joined)e.Wire(uno,"5V",uno,"3V3");
        var issues=e.Check(new SourceConflictRule());Assert.Equal(joined,issues.Count>0);
        if(joined)Assert.Contains("разных напряжений",issues[0].Message);
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void BatteryShort(bool shorted)
    {
        using var e=new ElectricalTestCircuit();var b=e.Add("battery");if(shorted)e.Wire(b,"+",b,"-");
        Assert.Equal(shorted,e.Check(new BatteryShortRule()).Count>0);
    }
    [Theory]
    [InlineData("5V",true)][InlineData("3V3",false)]
    public void EspGpioOvervoltage(string source,bool expected)
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var esp=e.Add("esp32-devkit");e.Wire(uno,source,esp,"GPIO34");
        Assert.Equal(expected,e.Check(new EspOvervoltageRule()).Count>0);
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void ModulePowerIsRequiredButBoardOutputsMayBeFree(bool connected)
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var esp=e.Add("esp32-devkit");
        Assert.Empty(e.Check(new DisconnectedSupplyRule()));var sensor=e.Add("dht11");
        if(connected){e.Wire(uno,"5V",sensor,"VCC");e.Wire(uno,"GND",sensor,"GND");}
        var issues=e.Check(new DisconnectedSupplyRule());Assert.Equal(!connected,issues.Count>0);
        Assert.DoesNotContain(issues,i=>i.Components.Contains(uno.Model.Id)||i.Components.Contains(esp.Model.Id));
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void RequiredI2cPins(bool connected)
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var lcd=e.Add("lcd1602-i2c");
        if(connected){e.Wire(uno,"SDA",lcd,"SDA");e.Wire(uno,"SCL",lcd,"SCL");}
        Assert.Equal(!connected,e.Check(new RequiredPinRule()).Count>0);
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void I2cConflictRequiresBothSharedLines(bool sameBus)
    {
        using var e=new ElectricalTestCircuit();var a=e.Add("lcd1602-i2c");var b=e.Add("lcd1602-i2c");
        Assert.True(b.Model.TryUpdateProperties(b.Model.Value, null, a.Model.I2cAddress, out var error), error);
        e.Wire(a,"SDA",b,"SDA");if(sameBus)e.Wire(a,"SCL",b,"SCL");
        Assert.Equal(sameBus,e.Check(new I2cAddressRule()).Count>0);
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void LedNeedsSeriesResistorNotParallelDecoration(bool series)
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var led=e.Add("led");var r=e.Add("resistor");
        if(series){e.Wire(uno,"5V",r,"1");e.Wire(r,"2",led,"A");}
        else {e.Wire(uno,"5V",led,"A");e.Wire(r,"1",led,"A");e.Wire(r,"2",led,"K");}
        e.Wire(led,"K",uno,"GND");Assert.Equal(!series,e.Check(new LedResistorRule()).Count>0);
    }
    [Fact]
    public void GpioLedWithReturnResistorDoesNotRaiseMissingResistor()
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var led=e.Add("led");var r=e.Add("resistor");
        e.Wire(uno,"D3",led,"A");e.Wire(led,"K",r,"1");e.Wire(r,"2",uno,"GND");
        Assert.Empty(e.Check(new LedResistorRule()));
    }

}
