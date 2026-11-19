using System.Collections.Immutable;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Simulation.Gpio;
using CircuitEditor.Simulation.Instruments;

namespace CircuitEditor.Tests;

public sealed class SimulationInstrumentTests
{
    [Fact]
    public void ScopeAndLogicKeepBoundedHistoryAndObservePwmWithoutMoreDcSolves()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var resistor = design.Add("resistor");
        design.Wire(uno, "D9", resistor, "1");
        design.Wire(resistor, "2", uno, "GND");
        var session = SimulationScenarioTests.Create(design);
        var pin = Terminal(uno, "D9");
        session.AssignProbe(InstrumentKind.Scope, 0, pin);
        session.AssignProbe(InstrumentKind.Logic, 7, pin);
        Assert.False(session.AssignProbe(InstrumentKind.Logic, 8, pin));
        Assert.True(session.SetPin(new(uno.Model.Id, "D9"), new(GpioMode.Pwm, Duty: .25, PeriodMicroseconds: 2000)));
        var first = session.Step();
        var solves = session.Circuit.SolveCount;
        Assert.Contains(first.Instruments[InstrumentKind.Scope].History, sample => sample.Voltages[0] > 4);
        Assert.Contains(first.Instruments[InstrumentKind.Scope].History, sample => sample.Voltages[0] == 0);
        Assert.Contains(first.Instruments[InstrumentKind.Logic].History, sample => sample.Levels[7] == "1");
        Assert.Contains(first.Instruments[InstrumentKind.Logic].History, sample => sample.Levels[7] == "0");

        for (var i = 0; i < 450; i++) session.Advance();
        var samples = session.Observe().Instruments[InstrumentKind.Scope].History;
        Assert.Equal(TraceHistory.DefaultCapacity, samples.Length);
        Assert.Equal(451_000, samples[^1].Microseconds);
        Assert.Equal(100, samples[1].Microseconds - samples[0].Microseconds);
        Assert.True(samples[0].Microseconds > 100);
        Assert.Equal(solves, session.Circuit.SolveCount);
    }

    [Fact]
    public void MeterRespectsDirectionAndDifferentFloatingReferences()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var resistor = design.Add("resistor");
        var battery = design.Add("battery");
        design.Wire(uno, "5V", resistor, "1");
        design.Wire(resistor, "2", uno, "GND");
        var session = SimulationScenarioTests.Create(design);
        session.AssignProbe(InstrumentKind.Meter, 0, Terminal(resistor, "2"));
        session.AssignProbe(InstrumentKind.Meter, 1, Terminal(resistor, "1"));
        var reading = session.Observe().Instruments[InstrumentKind.Meter];
        Assert.Equal(-5, reading.Volts);
        Assert.True(reading.Amps < 0);

        session.AssignProbe(InstrumentKind.Meter, 1, Terminal(battery, "-"));
        reading = session.Observe().Instruments[InstrumentKind.Meter];
        Assert.Null(reading.Volts);
        Assert.Null(reading.Amps);
    }

    [Fact]
    public void ConflictingGpioAndOverloadAreAddressableAndDoNotRepeat()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        design.Wire(uno, "D9", uno, "D10");
        var session = SimulationScenarioTests.Create(design);
        session.SetPin(new(uno.Model.Id, "D9"), new(GpioMode.Output, true));
        session.SetPin(new(uno.Model.Id, "D10"), new(GpioMode.Output));
        var snapshot = session.Step();
        Assert.Contains(snapshot.Diagnostics, issue => issue.Code == "SIM_OUTPUT_CONFLICT");
        var high = Assert.Single(snapshot.Diagnostics.Where(issue => issue.Code == "SIM_GPIO_ABSOLUTE" && issue.PinId == "D9"));
        Assert.Equal(.1, high.Measured!.Value, 7);
        Assert.Equal(.04, high.Limit);
        Assert.Equal(DigitalLevel.X, session.Circuit.ReadDigital(new(uno.Model.Id, "D9"), 1_000));
        for (var i = 0; i < 10; i++) session.Step();
        Assert.Equal(snapshot.Diagnostics.ToArray(), session.State.Diagnostics.ToArray());
    }

    [Fact]
    public void InputOnlyReportsDiagnosticAndSerialDoesNotDropRepeatedText()
    {
        using var design = new ElectricalTestCircuit();
        var esp = design.Add("esp32-devkit");
        var config = new ScenarioConfiguration("serial", ImmutableDictionary<string, PinReference>.Empty,
            ImmutableDictionary<string, double>.Empty.Add("periodMs", 2), Text: "Повтор");
        var session = SimulationScenarioTests.Create(design, config);
        Assert.False(session.SetPin(new(esp.Model.Id, "GPIO34"), new(GpioMode.Output, true)));
        for (var i = 0; i < 8; i++) session.Step();
        Assert.Contains(session.State.Diagnostics, problem => problem.Code == "SIM_INPUT_ONLY");
        var output = new OutputViewModel();
        output.ReceiveSerial(session.State);
        output.ReceiveSerial(session.State);
        Assert.Equal(5, output.Lines.Count);
        Assert.All(output.Lines, line => Assert.Contains("Повтор", line));
        Assert.Equal(new long[] { 0, 2_000, 4_000, 6_000, 8_000 }, session.State.Serial.Select(line => line.Microseconds));
    }

    private static WireEndpoint Terminal(ComponentViewModel part, string id) =>
        WireEndpoint.Contact(part.Model.Id, part.Footprint.Contacts.First(p => p.ElectricalPinId == id).Id);
}
