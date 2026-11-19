using System.Collections.Immutable;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Tests;

public sealed class SimulationDeviceTests
{
    [Theory]
    [InlineData("arduino-uno")]
    [InlineData("arduino-nano")]
    public void BuiltinLedBlinksOnD13WithoutExternalWiring(string type)
    {
        using var design = new ElectricalTestCircuit();
        var board = design.Add(type);
        ScenarioConfiguration Blink(string pin) => new("blink",
            ImmutableDictionary<string, PinReference>.Empty.Add("LED_PIN", new(board.Model.Id, pin)),
            ImmutableDictionary<string, double>.Empty.Add("periodMs", 4));

        var session = SimulationScenarioTests.Create(design, Blink("D13"));
        Assert.Equal(1, session.Step().Components[board.Model.Id].Intensity);
        Assert.Equal(0, session.Step().Components[board.Model.Id].Intensity);
        var otherPin = SimulationScenarioTests.Create(design, Blink("D12"));
        Assert.Equal(0, otherPin.Step().Components[board.Model.Id].Intensity);
    }

    [Fact]
    public void SevenSegmentTurnsOnOnlyWiredSegments()
    {
        using var design = new ElectricalTestCircuit();
        var board = design.Add("arduino-uno");
        var digit = design.Add("seven-segment");
        var resistor = design.Add("resistor");
        design.Wire(board, "D9", resistor, "1");
        design.Wire(resistor, "2", digit, "G");
        design.Wire(digit, "COM", board, "GND");
        var session = SimulationScenarioTests.Create(design);
        Assert.Equal(0, session.State.Components[digit.Model.Id].Segments);
        session.SetPin(new(board.Model.Id, "D9"), new(GpioMode.Output, true));
        Assert.Equal(1 << 6, session.Step().Components[digit.Model.Id].Segments);
    }

    [Fact]
    public void NpnSaturatesWithBaseDriveAndCutsOffWithLowInput()
    {
        using var design = new ElectricalTestCircuit();
        var board = design.Add("arduino-uno");
        var transistor = design.Add("npn");
        var baseResistor = design.Add("resistor");
        var load = design.Add("resistor");
        design.Wire(board, "D9", baseResistor, "1");
        design.Wire(baseResistor, "2", transistor, "B");
        design.Wire(board, "5V", load, "1");
        design.Wire(load, "2", transistor, "C");
        design.Wire(board, "GND", transistor, "E");
        var session = SimulationScenarioTests.Create(design);
        var output = new PinReference(board.Model.Id, "D9");
        var collector = new PinReference(transistor.Model.Id, "C");
        session.SetPin(output, new(GpioMode.Output, true));
        var active = session.Step();
        Assert.False(active.IsHalted);
        Assert.InRange(session.Circuit.Voltage(collector)!.Value, .2, .4);
        session.SetPin(output, new(GpioMode.Output));
        Assert.False(session.Step().IsHalted);
        Assert.Equal(5, session.Circuit.Voltage(collector)!.Value, 6);
    }

    [Fact]
    public void I2cScreenUsesPoweredTextBufferAndWrapsCharacterRows()
    {
        using var design = new ElectricalTestCircuit();
        var board = design.Add("arduino-uno");
        var display = design.Add("lcd1602-i2c");
        design.Wire(board, "5V", display, "VCC");
        design.Wire(board, "GND", display, "GND");
        var selection = new ScenarioConfiguration("display", ImmutableDictionary<string, PinReference>.Empty,
            ImmutableDictionary<string, double>.Empty, display.Model.Id, "ABCDEFGHIJKLMNOPQ");
        var state = SimulationScenarioTests.Create(design, selection).Step();
        var visual = state.Components[display.Model.Id];
        Assert.True(visual.Active);
        Assert.Equal("ABCDEFGHIJKLMNOP\nQ", visual.Text);
    }

    [Fact]
    public void UltrasonicEchoUsesDistanceAndVirtualTriggerDuration()
    {
        using var design = new ElectricalTestCircuit();
        var board = design.Add("arduino-uno");
        var sensor = design.Add("hc-sr04");
        design.Wire(board, "5V", sensor, "VCC");
        design.Wire(board, "GND", sensor, "GND");
        design.Wire(board, "D9", sensor, "TRIG");
        design.Wire(board, "D6", sensor, "ECHO");
        var session = SimulationScenarioTests.Create(design);
        var trigger = new PinReference(board.Model.Id, "D9");
        var echo = new PinReference(board.Model.Id, "D6");
        session.SetInput(sensor.Model.Id, CircuitEditor.Core.Inputs.ComponentInput.Distance, 10);
        session.SetPin(trigger, new(GpioMode.Output));
        session.Schedule(100, () => session.SetPin(trigger, new(GpioMode.Output, true)));
        session.Schedule(120, () => session.SetPin(trigger, new(GpioMode.Output)));
        var levels = new List<DigitalLevel>();
        foreach (var time in new long[] { 119, 130, 699, 701 })
            session.Schedule(time, () => levels.Add(session.Circuit.ReadDigital(echo, session.ModelTime)));
        Assert.False(session.Step().IsHalted);
        Assert.Equal(new[] { DigitalLevel.Low, DigitalLevel.High, DigitalLevel.High, DigitalLevel.Low }, levels);
        Assert.Equal(580, session.State.Components[sensor.Model.Id].Measurements!["EchoUs"]);
    }
}
