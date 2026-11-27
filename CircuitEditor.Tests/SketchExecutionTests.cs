using CircuitEditor.Core.Code;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SketchExecutionTests
{
    internal const string Blink = """
        // This statement is a comment: digitalWrite(LED_BUILTIN, LOW);
        const int led = LED_BUILTIN;
        #define PERIOD 3
        void setup() { pinMode(led, OUTPUT); }
        void loop() {
          digitalWrite(led, HIGH);
          delay(PERIOD);
          digitalWrite(led, LOW);
          delay(PERIOD);
        }
        """;

    [Fact]
    public void SketchDrivesBuiltInLedExternalLedAndScopeAtModelTimes()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var led = design.Add("led");
        var resistor = design.Add("resistor");
        design.Wire(uno, "D13", resistor, "1");
        design.Wire(resistor, "2", led, "A");
        design.Wire(led, "K", uno, "GND");
        var document = new SketchDocument(Blink);
        design.Project.UpdateSketch(document);
        Assert.Equal(0, SimulationScenarioTests.Create(design).State.Components[led.Model.Id].Intensity);

        var session = SimulationScenarioTests.Create(design, sketch: document);
        Assert.False(session.State.IsHalted);
        Assert.Equal(1, session.State.Components[uno.Model.Id].Intensity);
        Assert.True(session.State.Components[led.Model.Id].Intensity > 0);
        session.AssignProbe(InstrumentKind.Scope, 0, WireEndpoint.Contact(uno.Model.Id, "D13"));
        session.AssignProbe(InstrumentKind.Meter, 0, WireEndpoint.Contact(uno.Model.Id, "D13"));
        session.AssignProbe(InstrumentKind.Meter, 1, WireEndpoint.Contact(uno.Model.Id, "GND"));
        Assert.True(session.Observe().Instruments[InstrumentKind.Meter].Volts > 4);
        Assert.True(session.Step().Components[led.Model.Id].Intensity > 0);
        Assert.Equal(0, session.Step().Components[led.Model.Id].Intensity);
        session.Step();
        session.Step();
        var restarted = session.Step();
        Assert.True(restarted.Components[led.Model.Id].Intensity > 0);
        Assert.Contains(restarted.Instruments[InstrumentKind.Scope].History, point => point.Voltages[0] > 4);
        Assert.Contains(restarted.Instruments[InstrumentKind.Scope].History, point => point.Voltages[0] == 0);
    }

    [Fact]
    public void LoopReadsTheSimulatedButtonAndKeepsSetupPinMode()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var button = design.Add("button");
        design.Wire(uno, "D2", button, "1");
        design.Wire(button, "2", uno, "GND");
        var session = SimulationScenarioTests.Create(design, sketch: new("""
            #include <Arduino.h>
            void setup() {
                pinMode(2, INPUT_PULLUP);
                pinMode(13, OUTPUT);
            }
            void loop() {
                if (digitalRead(2) == LOW) digitalWrite(13, HIGH);
                else digitalWrite(13, LOW);
            }
            """));
        Assert.False(session.State.IsHalted);
        Assert.Equal(0, session.State.Components[uno.Model.Id].Intensity);
        Assert.True(session.SetInput(button.Model.Id, ComponentInput.ButtonPressed, 1));
        Assert.Equal(1, session.Step().Components[uno.Model.Id].Intensity);
        Assert.True(session.SetInput(button.Model.Id, ComponentInput.ButtonPressed, 0));
        Assert.Equal(0, session.Step().Components[uno.Model.Id].Intensity);
    }

    [Fact]
    public void RunsExistingGeneratedBlinkWithMillisAndPersistentVariables()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var led = design.Add("led");
        var resistor = design.Add("resistor");
        design.Wire(uno, "D13", resistor, "1");
        design.Wire(resistor, "2", led, "A");
        design.Wire(led, "K", uno, "GND");
        var generated = new ArduinoCodeGenerator(new JsonCodeTemplateRepository())
            .Generate(design.Project.Model, design.Nets(), new());
        Assert.True(generated.Success);
        Assert.Contains("millis()", generated.Text);
        var session = SimulationScenarioTests.Create(design, sketch: new(generated.Text));
        Assert.False(session.State.IsHalted, session.State.Failure);
        Assert.Equal(0, session.State.Components[led.Model.Id].Intensity);
        for (var step = 0; step < 998; step++)
        {
            session.Advance();
            if (session.ModelTime == 500_000)
                Assert.True(session.Observe().Components[led.Model.Id].Intensity > 0);
        }
        Assert.Equal(999_000, session.ModelTime);
        Assert.Equal(0, session.Step().Components[led.Model.Id].Intensity);
    }

    [Theory]
    [InlineData(BoardKind.Uno, "arduino-uno", "Servo.attach(9);", "Servo")]
    [InlineData(BoardKind.Esp32, "esp32-devkit", "pinMode(34, OUTPUT);", "только на вход")]
    [InlineData(BoardKind.Uno, "arduino-uno", "analogWrite(2, 128);", "ШИМ")]
    public void InvalidSketchStopsWithAnEditorPosition(BoardKind board, string definition, string statement, string explanation)
    {
        using var design = new ElectricalTestCircuit();
        design.Add(definition);
        var session = SimulationScenarioTests.Create(design, sketch: new($"void setup() {{\n  {statement}\n}}\nvoid loop() {{}}", board));
        Assert.True(session.State.IsHalted);
        var error = Assert.Single(session.State.Diagnostics);
        Assert.Equal("CODE_RUN", error.Code);
        Assert.Equal(2, error.SketchLine);
        Assert.Contains(explanation, error.Message);
    }

    [Fact]
    public void MultipleBoardsRequireAnInstanceAndSelectedBoardGetsTheOutput()
    {
        using var design = new ElectricalTestCircuit();
        var first = design.Add("arduino-nano");
        var second = design.Add("arduino-nano");
        var document = new SketchDocument(Blink, BoardKind.Nano);
        var unresolved = SimulationScenarioTests.Create(design, sketch: document);
        Assert.True(unresolved.State.IsHalted);
        Assert.Contains("выберите экземпляр", unresolved.State.Failure);
        var chosen = SimulationScenarioTests.Create(design, sketch: document with { ControllerComponentId = second.Model.Id });
        Assert.Equal(0, chosen.State.Components[first.Model.Id].Intensity);
        Assert.Equal(1, chosen.State.Components[second.Model.Id].Intensity);
    }

    [Fact]
    public void TwoLcdObjectsShareTheBusAndKeepIndependentTextAndCursors()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var first = design.Add("lcd1602-i2c");
        var second = design.Add("lcd1602-i2c");
        Assert.Equal("0x27", first.Model.I2cAddress);
        Assert.Equal("0x26", second.Model.I2cAddress);
        ConnectLcd(design, uno, first);
        ConnectLcd(design, uno, second);

        var session = SimulationScenarioTests.Create(design, sketch: new("""
            #include <Wire.h>
            #include <LiquidCrystal_I2C.h>
            LiquidCrystal_I2C lcd1(0x27, 16, 2);
            LiquidCrystal_I2C lcd2(0x26, 16, 2);

            void setup() {
                Wire.begin();
                lcd1.init();
                lcd2.init();
                lcd1.backlight();
                lcd2.backlight();
                lcd1.print("First screen");
                lcd2.print("Second screen");
                lcd1.setCursor(0, 1);
                lcd2.setCursor(2, 1);
                lcd1.print("Line one");
                lcd2.print("Line two");
            }

            void loop() {
                delay(3);
                lcd2.clear();
                lcd2.print("Updated");
                delay(10);
            }
            """));

        Assert.False(session.State.IsHalted, session.State.Failure);
        Assert.Equal("First screen".PadRight(16) + "\nLine one", session.State.Components[first.Model.Id].Text);
        Assert.Equal("Second screen".PadRight(16) + "\n  Line two", session.State.Components[second.Model.Id].Text);
        session.Step();
        session.Step();
        var next = session.Step();
        Assert.Equal("First screen".PadRight(16) + "\nLine one", next.Components[first.Model.Id].Text);
        Assert.Equal("Updated", next.Components[second.Model.Id].Text);
    }

    [Theory]
    [InlineData(false, "обращаются к одному LCD")]
    [InlineData(true, "укажите разные I²C-адреса")]
    public void ReusedLcdAddressExplainsWhyTheSecondDisplayCannotBeAddressed(bool samePhysicalAddress, string message)
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var first = design.Add("lcd1602-i2c");
        var second = design.Add("lcd1602-i2c");
        ConnectLcd(design, uno, first);
        ConnectLcd(design, uno, second);
        if (samePhysicalAddress)
            Assert.True(second.Model.TryUpdateProperties(second.Model.Value, null, first.Model.I2cAddress, out var error), error);
        var session = SimulationScenarioTests.Create(design, sketch: new("""
            LiquidCrystal_I2C lcd1(0x27, 16, 2);
            LiquidCrystal_I2C lcd2(0x27, 16, 2);
            void setup() {}
            void loop() {}
            """));

        Assert.True(session.State.IsHalted);
        var diagnostic = Assert.Single(session.State.Diagnostics.Where(item => item.Code == "CODE_RUN"));
        Assert.Contains(message, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0x27", diagnostic.Message);
    }

    private static void ConnectLcd(ElectricalTestCircuit design, CircuitEditor.App.ViewModels.ComponentViewModel uno,
        CircuitEditor.App.ViewModels.ComponentViewModel lcd)
    {
        design.Wire(uno, "5V", lcd, "VCC");
        design.Wire(uno, "GND", lcd, "GND");
        design.Wire(uno, "SDA", lcd, "SDA");
        design.Wire(uno, "SCL", lcd, "SCL");
    }
}
