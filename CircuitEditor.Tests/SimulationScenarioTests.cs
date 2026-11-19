using System.Collections.Immutable;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Gpio;
using CircuitEditor.Simulation.Scenarios;

namespace CircuitEditor.Tests;

public sealed class SimulationScenarioTests
{
    [Fact]
    public void BlinkUsesModelPeriodAndProducesIdenticalRuns()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var resistor = design.Add("resistor");
        var led = design.Add("led");
        design.Wire(uno, "D13", resistor, "1");
        design.Wire(resistor, "2", led, "A");
        design.Wire(led, "K", uno, "GND");
        var configuration = Configure("blink", ("LED_PIN", new(uno.Model.Id, "D13"))) with
        {
            Numbers = ImmutableDictionary<string,double>.Empty.Add("periodMs", 8).Add("duty", 0.5)
        };
        double[] Run()
        {
            var session = Create(design, configuration);
            return Enumerable.Range(0, 12).Select(_ => session.Step().Components[led.Model.Id].Intensity).ToArray();
        }
        var first = Run();
        Assert.Equal(first, Run());
        Assert.All(first[..3], value => Assert.True(value > 0));
        Assert.All(first[3..7], value => Assert.Equal(0, value));
        Assert.Equal(first[0], first[7]);
    }

    [Fact]
    public void RemovedBoardOrPinPreventsInvalidScenario()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        foreach (var pin in new PinReference[] { new(Guid.NewGuid(), "D13"), new(uno.Model.Id, "D99") })
        {
            var session = Create(design, Configure("blink", ("LED_PIN", pin)));
            Assert.True(session.Step().IsHalted);
            var diagnostic = Assert.Single(session.State.Diagnostics);
            Assert.Equal("SIM_BINDING", diagnostic.Code);
            Assert.Equal(pin, new PinReference(diagnostic.ComponentId!.Value, diagnostic.PinId!));
            Assert.Equal(0, session.State.Microseconds);
        }
    }

    [Fact]
    public void ButtonPullupChangesLedWithoutEditingProject()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var button = design.Add("button");
        var led = design.Add("led");
        var resistor = design.Add("resistor");
        design.Wire(uno, "D2", button, "1");
        design.Wire(button, "2", uno, "GND");
        design.Wire(uno, "D13", resistor, "1");
        design.Wire(resistor, "2", led, "A");
        design.Wire(led, "K", uno, "GND");
        var session = Create(design, Configure("button-led", ("BUTTON_PIN", new(uno.Model.Id, "D2")), ("LED_PIN", new(uno.Model.Id, "D13"))));
        Assert.Equal(0, session.Step().Components[led.Model.Id].Intensity);
        Assert.True(session.SetInput(button.Model.Id, ComponentInput.ButtonPressed, 1));
        Assert.True(session.Step().Components[led.Model.Id].Intensity > 0);
        Assert.Equal(0, button.Model.Inputs.Values[ComponentInput.ButtonPressed]);
        Assert.True(session.SetInput(button.Model.Id, ComponentInput.ButtonPressed, 0));
        Assert.Equal(0, session.Step().Components[led.Model.Id].Intensity);
    }

    [Fact]
    public void PotentiometerDrivesPwmBrightness()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var pot = design.Add("potentiometer");
        var led = design.Add("led");
        var resistor = design.Add("resistor");
        design.Wire(pot, "A", uno, "5V");
        design.Wire(pot, "B", uno, "GND");
        design.Wire(pot, "W", uno, "A0");
        design.Wire(uno, "D9", resistor, "1");
        design.Wire(resistor, "2", led, "A");
        design.Wire(led, "K", uno, "GND");
        var session = Create(design, Configure("pot-pwm", ("POT_PIN", new(uno.Model.Id, "A0")),
            ("LED_PIN", new(uno.Model.Id, "D9"))));
        session.SetPotentiometer(pot.Model.Id, PotentiometerPosition.MinimumAngle);
        var first = session.Step().Components[led.Model.Id].Intensity;
        session.SetPotentiometer(pot.Model.Id, PotentiometerPosition.MaximumAngle);
        var last = session.Step().Components[led.Model.Id].Intensity;
        Assert.True(Math.Abs(first - last) > 0.05);
        Assert.Equal(0, Math.Min(first, last), 4);
    }

    [Fact]
    public void PwmPulseWidthDrivesServoAngle()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var servo = design.Add("servo");
        design.Wire(uno, "5V", servo, "VCC");
        design.Wire(uno, "GND", servo, "GND");
        design.Wire(uno, "D9", servo, "SIG");
        var session = Create(design);
        Assert.True(session.SetPin(new(uno.Model.Id, "D9"), new(GpioMode.Pwm, Duty: 0.075, PeriodMicroseconds: 20_000)));
        for (var i = 0; i < 200; i++) session.Step();
        Assert.Equal(90, session.State.Components[servo.Model.Id].Angle, 4);
    }

    [Fact]
    public void RelayChangesTopologyAndFeedbackStopsOnIterationLimit()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var relay = design.Add("relay");
        design.Wire(uno, "5V", relay, "COIL+");
        design.Wire(uno, "GND", relay, "COIL-");
        var stable = Create(design);
        Assert.False(stable.State.IsHalted);
        Assert.True(stable.State.Components[relay.Model.Id].Active);
        Assert.Equal(stable.Circuit.Nets.Find(relay.Model.Id, "COM"), stable.Circuit.Nets.Find(relay.Model.Id, "NO"));

        using var feedback = new ElectricalTestCircuit();
        var battery = feedback.Add("battery");
        var switchRelay = feedback.Add("relay");
        feedback.Wire(battery, "+", switchRelay, "COM");
        feedback.Wire(switchRelay, "NC", switchRelay, "COIL+");
        feedback.Wire(battery, "-", switchRelay, "COIL-");
        var oscillating = Create(feedback);
        Assert.True(oscillating.State.IsHalted);
        Assert.Equal("SIM_OSCILLATION", Assert.Single(oscillating.State.Diagnostics).Code);
        Assert.InRange(oscillating.Circuit.SolveCount, 2, 32);
    }

    [Fact]
    public void UnbranchedJumperReportsSignedCurrentAndOhmicLoad()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var resistor = design.Add("resistor");
        design.Wire(uno, "5V", resistor, "1");
        design.Wire(uno, "GND", resistor, "2");
        var session = Create(design);
        var wires = design.Project.Model.Circuit.Wires;
        var supply = session.State.Wires[wires[0].Id];
        var ground = session.State.Wires[wires[1].Id];
        Assert.Equal(5, supply.Voltage);
        Assert.Equal(0, ground.Voltage);
        Assert.True(supply.Current > 0);
        Assert.Equal(supply.Current!.Value, -ground.Current!.Value, 9);
    }

    private static ScenarioConfiguration Configure(string template, params (string Name, PinReference Pin)[] pins) =>
        new(template, pins.ToImmutableDictionary(p => p.Name, p => p.Pin), ImmutableDictionary<string,double>.Empty);

    internal static SimulationSession Create(ElectricalTestCircuit design, ScenarioConfiguration? scenario = null)
    {
        var electrical = new JsonElectricalLibraryRepository();
        var factory = new SimulationSessionFactory(new NetlistBuilder(electrical), new DcSolver(electrical),
            new JsonSimulationModelRepository(), new ScenarioRunner(new JsonScenarioRepository()),
            [new LedBehavior(), new RelayBehavior(), new ServoBehavior(), new SensorBehavior()]);
        return factory.Create(design.Project.Model, scenario);
    }
}
