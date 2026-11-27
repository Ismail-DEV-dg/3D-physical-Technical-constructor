using CircuitEditor.Core.Code;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class CodeGeneratorTests
{
    private static CodeGenerationResult Generate(ElectricalTestCircuit e,SketchDocument? sketch=null)=>
        new ArduinoCodeGenerator(new JsonCodeTemplateRepository()).Generate(e.Project.Model,e.Nets(),sketch??new());
    [Fact]
    public void LedThroughResistorAndButtonHavePinsModesAndPreserveManualCode()
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var led=e.Add("led");var resistor=e.Add("resistor");var button=e.Add("button");
        e.Wire(uno,"D9",resistor,"1");e.Wire(resistor,"2",led,"A");e.Wire(led,"K",uno,"GND");
        e.Wire(uno,"D2",button,"1");e.Wire(button,"2",uno,"GND");
        var first=Generate(e);Assert.True(first.Success,string.Join(";",first.Problems));
        Assert.Contains("#define LED1_PIN 9",first.Text);Assert.Contains("pinMode(LED1_PIN, OUTPUT);",first.Text);
        Assert.Contains("INPUT_PULLUP",first.Text);
        var manual="// manual prefix\n"+first.Text+"\n// manual suffix\n";
        var updated=Generate(e,new(Text:manual));Assert.True(updated.Success);Assert.StartsWith("// manual prefix\n",updated.Text);Assert.EndsWith("\n// manual suffix\n",updated.Text);
        Assert.Equal(updated.Text,Generate(e,new(Text:updated.Text)).Text);
        Assert.False(Generate(e,new(Text:"void setup() { /* manual */ }")).Success);
    }
    [Theory]
    [InlineData("dht11","DATA","D4","DHT.h","DHT11")]
    [InlineData("dht22","DATA","D4","DHT.h","DHT22")]
    [InlineData("servo","SIG","D9","Servo.h",".attach(")]
    public void SupportedModulesProduceExpectedFragments(string id,string pin,string boardPin,string header,string fragment)
    {
        using var e=new ElectricalTestCircuit();var board=e.Add("arduino-uno");var component=e.Add(id);e.Wire(board,boardPin,component,pin);
        var result=Generate(e);Assert.True(result.Success);Assert.Contains("#include <"+header+">",result.Text);Assert.Contains(fragment,result.Text);
    }
    [Theory]
    [InlineData("lcd1602-i2c","LiquidCrystal_I2C.h","0x27")]
    [InlineData("oled-ssd1306","Adafruit_SSD1306.h","0x3C")]
    public void I2cUsesActualConnectedPinsAndAddress(string id,string header,string address)
    {
        using var e=new ElectricalTestCircuit();var board=e.Add("arduino-uno");var module=e.Add(id);
        e.Wire(board,"A4",module,"SDA");e.Wire(board,"A5",module,"SCL");
        var result=Generate(e);Assert.Contains(header,result.Text);Assert.Contains(address,result.Text);Assert.Contains("Wire.begin();",result.Text);
    }
    [Theory]
    [InlineData("arduino-uno",BoardKind.Uno,"D2",true)]
    [InlineData("arduino-nano",BoardKind.Nano,"D9",true)]
    [InlineData("esp32-devkit",BoardKind.Esp32,"GPIO18",true)]
    [InlineData("esp32-devkit",BoardKind.Esp32,"GPIO34",false)]
    public void ServoUsesTimedPulsesOnOutputPins(string boardId,BoardKind kind,string pin,bool allowed)
    {
        using var e=new ElectricalTestCircuit();var board=e.Add(boardId);var servo=e.Add("servo");e.Wire(board,pin,servo,"SIG");
        var result=Generate(e,new(Board:kind));Assert.True(result.Success);
        if(allowed){Assert.Contains(kind==BoardKind.Esp32?"<ESP32Servo.h>":"<Servo.h>",result.Text);Assert.Contains(".attach(",result.Text);}
        else{Assert.DoesNotContain(".attach(",result.Text);Assert.NotEmpty(result.Problems);}
    }
    [Fact]
    public void UnsupportedAndAmbiguousComponentsAreNotInventedAndLayoutDoesNotStaleCode()
    {
        using var e=new ElectricalTestCircuit();e.Add("arduino-uno");e.Add("seven-segment");
        var first=Generate(e);Assert.Contains("шаблон не поддерживается",first.Text);
        e.Project.Components[0].MockupPlacement.MoveTo(300,400);Assert.Equal(first.CircuitSignature,Generate(e).CircuitSignature);
        e.Add("arduino-uno");Assert.False(Generate(e).Success);
        Assert.True(Generate(e,new(ControllerComponentId:e.Project.Components[0].Model.Id)).Success);
    }
    [Fact]
    public void BrokenOrDuplicateMarkersAreRejectedAtomically()
    {
        using var e=new ElectricalTestCircuit();e.Add("arduino-uno");var first=Generate(e);
        foreach(var text in new[]{first.Text.Replace("// </auto>","// broken"),first.Text+"\n// <auto>\n// section: globals\n// </auto>"})
        {var result=Generate(e,new(Text:text));Assert.False(result.Success);Assert.Equal(text,result.Text);}
        Assert.True(new JsonCodeCompletionRepository().Load().Success);
    }
    [Fact]
    public void RelayUsesAnExplicitNpnDriverAndWifiIsOnlyIncludedForActualEsp32Code()
    {
        using var e=new ElectricalTestCircuit();var board=e.Add("esp32-devkit");var relay=e.Add("relay");var npn=e.Add("npn");var resistor=e.Add("resistor");
        e.Wire(board,"GPIO18",resistor,"1");e.Wire(resistor,"2",npn,"B");e.Wire(npn,"C",relay,"COIL-");
        var first=Generate(e,new(Board:BoardKind.Esp32));Assert.Contains("#define K1_PIN 18",first.Text);Assert.Contains("pinMode(K1_PIN, OUTPUT)",first.Text);Assert.DoesNotContain("WiFi.h",first.Text);
        var manual=first.Text+"\nvoid network() { WiFi.begin(); }\n";
        var updated=Generate(e,new(Text:manual,Board:BoardKind.Esp32));Assert.Contains("#include <WiFi.h>",updated.Text);
    }
}
