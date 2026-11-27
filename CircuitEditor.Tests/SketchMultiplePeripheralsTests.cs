using System.Text;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Tests;

public sealed class SketchMultiplePeripheralsTests
{
    private const int DeviceCount = 10;

    [Fact]
    public void TenLcdAndTenOledDisplaysKeepSeparateAddressesAndTextOnOneBus()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var lcds = new List<ComponentViewModel>();
        var oleds = new List<ComponentViewModel>();
        var declarations = new StringBuilder("""
            #include <Wire.h>
            #include <LiquidCrystal_I2C.h>
            #include <Adafruit_GFX.h>
            #include <Adafruit_SSD1306.h>

            """);
        var setup = new StringBuilder("void setup() { Wire.begin();\n");
        for (var i = 0; i < DeviceCount; i++)
        {
            var lcd = design.Add("lcd1602-i2c");
            var oled = design.Add("oled-ssd1306");
            lcds.Add(lcd);
            oleds.Add(oled);
            WireI2c(design, uno, lcd, "5V");
            WireI2c(design, uno, oled, "3V3");
            declarations.AppendLine($"LiquidCrystal_I2C lcd{i}({lcd.Model.I2cAddress}, 16, 2);");
            declarations.AppendLine($"Adafruit_SSD1306 oled{i}(128, 64, &Wire, -1);");
            setup.AppendLine($"lcd{i}.init(); lcd{i}.print(\"LCD {i}\");");
            setup.AppendLine($"if (!oled{i}.begin(SSD1306_SWITCHCAPVCC, {oled.Model.I2cAddress})) {{}}");
            setup.AppendLine($"oled{i}.clearDisplay(); oled{i}.setTextSize(1); oled{i}.setTextColor(SSD1306_WHITE);");
            setup.AppendLine($"oled{i}.setCursor(0, 0); oled{i}.print(\"OLED \"); oled{i}.print({i}); oled{i}.display();");
        }
        Assert.Equal(2 * DeviceCount, lcds.Concat(oleds).Select(part => part.Model.I2cAddress).Distinct().Count());
        var session = SimulationScenarioTests.Create(design, sketch: new($"{declarations}\n{setup}}}\nvoid loop() {{}}"));
        Assert.False(session.State.IsHalted, session.State.Failure);
        for (var i = 0; i < DeviceCount; i++)
        {
            Assert.Equal($"LCD {i}", session.State.Components[lcds[i].Model.Id].Text);
            Assert.Equal($"OLED {i}", session.State.Components[oleds[i].Model.Id].Text);
        }
    }

    [Fact]
    public void TenServosFollowTenSeparateAdcInputsOnEsp32()
    {
        using var design = new ElectricalTestCircuit();
        var esp = design.Add("esp32-devkit");
        int[] adcPins = [2, 4, 12, 13, 14, 15, 25, 26, 27, 32];
        int[] servoPins = [1, 3, 5, 16, 17, 18, 19, 21, 22, 23];
        var servos = new List<ComponentViewModel>();
        var declarations = new StringBuilder("#include <ESP32Servo.h>\n");
        var setup = new StringBuilder("void setup() { analogReadResolution(12);\n");
        var reads = new StringBuilder("void loop() {\n");
        var writes = new StringBuilder();
        for (var i = 0; i < DeviceCount; i++)
        {
            var pot = design.Add("potentiometer");
            var fraction = i / (double)(DeviceCount - 1);
            Assert.True(pot.Model.TrySetPotentiometerAngle(PotentiometerPosition.FromPercent(100 * fraction)));
            design.Wire(esp, "GND", pot, "A");
            design.Wire(esp, "3V3", pot, "B");
            design.Wire(esp, $"GPIO{adcPins[i]}", pot, "W");

            var servo = design.Add("servo");
            servos.Add(servo);
            design.Wire(esp, "5V", servo, "VCC");
            design.Wire(esp, "GND", servo, "GND");
            design.Wire(esp, $"GPIO{servoPins[i]}", servo, "SIG");
            declarations.AppendLine($"Servo motor{i};");
            setup.AppendLine($"motor{i}.attach({servoPins[i]});");
            reads.AppendLine($"const int adc{i} = analogRead({adcPins[i]});");
            writes.AppendLine($"motor{i}.write(map(adc{i}, 0, 4095, 0, 180));");
        }
        var source = $"{declarations}{setup}}}\n{reads}{writes}\ndelay(10); }}";
        var session = SimulationScenarioTests.Create(design, sketch: new(source, BoardKind.Esp32));
        Assert.False(session.State.IsHalted, session.State.Failure);
        for (var step = 0; step < 400; step++) session.Advance();
        var state = session.Observe();
        Assert.False(state.IsHalted, state.Failure);
        for (var i = 0; i < DeviceCount; i++)
        {
            var expected = i * 180d / (DeviceCount - 1);
            Assert.InRange(state.Components[servos[i].Model.Id].Angle, expected - 2, expected + 2);
        }
    }

    [Fact]
    public void TenParallelLcdsShareDataLinesWithIndependentEnablePins()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        string[] enablePins = ["D7", "D8", "D9", "D10", "D11", "D12", "D13", "A0", "A1", "A2"];
        string[] shared = ["RS", "D4", "D5", "D6", "D7"];
        var lcds = new List<ComponentViewModel>();
        var declarations = new StringBuilder("#include <LiquidCrystal.h>\n");
        var setup = new StringBuilder("void setup() {\n");
        for (var i = 0; i < DeviceCount; i++)
        {
            var lcd = design.Add("lcd1602");
            lcds.Add(lcd);
            for (var p = 0; p < shared.Length; p++) design.Wire(uno, $"D{2 + p}", lcd, shared[p]);
            design.Wire(uno, enablePins[i], lcd, "E");
            design.Wire(uno, "5V", lcd, "VDD");
            design.Wire(uno, "GND", lcd, "VSS");
            design.Wire(uno, "GND", lcd, "RW");
            var enable = enablePins[i].StartsWith('D') ? enablePins[i][1..] : enablePins[i];
            declarations.AppendLine($"LiquidCrystal lcd{i}(2, {enable}, 3, 4, 5, 6);");
            setup.AppendLine($"lcd{i}.begin(16, 2); lcd{i}.print(\"Module {i}\");");
        }
        var session = SimulationScenarioTests.Create(design, sketch: new($"{declarations}\n{setup}}}\nvoid loop() {{}}"));
        Assert.False(session.State.IsHalted, session.State.Failure);
        for (var i = 0; i < DeviceCount; i++)
            Assert.Equal($"Module {i}", session.State.Components[lcds[i].Model.Id].Text);
    }

    [Fact]
    public void TenUltrasonicSensorsReportTheirOwnEchoDurations()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        string[] pins = ["D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D10", "D11",
            "D12", "D13", "A0", "A1", "A2", "A3", "A4", "A5"];
        var setup = new StringBuilder("void setup() { Serial.begin(9600);\n");
        for (var i = 0; i < DeviceCount; i++)
        {
            var sensor = design.Add("hc-sr04");
            var (trigger, echo) = (pins[i * 2], pins[i * 2 + 1]);
            Assert.True(sensor.Model.TrySetInput(ComponentInput.Distance, 20 + 5 * i));
            design.Wire(uno, "5V", sensor, "VCC");
            design.Wire(uno, "GND", sensor, "GND");
            design.Wire(uno, trigger, sensor, "TRIG");
            design.Wire(uno, echo, sensor, "ECHO");
            setup.AppendLine($"pinMode({trigger}, OUTPUT); pinMode({echo}, INPUT);");
            setup.AppendLine($"digitalWrite({trigger}, HIGH); delayMicroseconds(12); digitalWrite({trigger}, LOW);");
            setup.AppendLine($"long width{i} = pulseIn({echo}, HIGH, 30000); Serial.println(width{i});");
        }
        var session = SimulationScenarioTests.Create(design, sketch: new($"{setup}}}\nvoid loop() {{}}"));
        for (var i = 0; i < 120; i++) session.Advance();
        var state = session.Observe();
        Assert.False(state.IsHalted, state.Failure);
        Assert.Equal(Enumerable.Range(0, DeviceCount).Select(i => ((20 + 5 * i) * 58).ToString()), state.Serial.Select(line => line.Text));
    }

    [Fact]
    public void TenDhtSensorsResolveByTheirSeparateDataPins()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var declarations = new StringBuilder("#include <DHT.h>\n");
        var setup = new StringBuilder("void setup() { Serial.begin(9600);\n");
        for (var i = 0; i < DeviceCount; i++)
        {
            var sensor = design.Add("dht22");
            Assert.True(sensor.Model.TrySetInput(ComponentInput.Temperature, 20 + i));
            design.Wire(uno, "5V", sensor, "VCC");
            design.Wire(uno, "GND", sensor, "GND");
            design.Wire(uno, $"D{i + 2}", sensor, "DATA");
            declarations.AppendLine($"DHT sensor{i}({i + 2}, DHT22);");
            setup.AppendLine($"sensor{i}.begin(); Serial.println(sensor{i}.readTemperature());");
        }
        var session = SimulationScenarioTests.Create(design, sketch: new($"{declarations}\n{setup}}}\nvoid loop() {{}}"));
        Assert.False(session.State.IsHalted, session.State.Failure);
        Assert.Equal(Enumerable.Range(20, DeviceCount).Select(i => i.ToString()), session.State.Serial.Select(line => line.Text));
    }

    private static void WireI2c(ElectricalTestCircuit design, ComponentViewModel board, ComponentViewModel display, string supply)
    {
        design.Wire(board, supply, display, "VCC");
        design.Wire(board, "GND", display, "GND");
        design.Wire(board, "SDA", display, "SDA");
        design.Wire(board, "SCL", display, "SCL");
    }
}
