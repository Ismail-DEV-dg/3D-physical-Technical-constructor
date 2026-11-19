using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Validation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Gpio;
using CircuitEditor.Simulation.Instruments;
using CircuitEditor.Simulation.Scenarios;

namespace CircuitEditor.Tests;

public sealed class ChipSimulationTests
{
    [Theory]
    [InlineData("270 Ω", false, .009, .012)]
    [InlineData("120 Ω", true, .018, .025)]
    public void LoadedTtlNandUsesSinkLimit(string resistance, bool overloaded, double minimum, double maximum)
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var led = design.Add("led");
        var resistor = design.Add("resistor");
        Assert.True(resistor.Model.TryUpdateProperties(resistance, null, null, out _));
        var chip = Add(design, ChipTemplateTests.Get("7400"));
        LinkChip(design, uno, "5V", chip, "14");
        LinkChip(design, uno, "GND", chip, "7");
        LinkChip(design, uno, "5V", chip, "1");
        LinkChip(design, uno, "5V", chip, "2");
        design.Wire(uno, "5V", resistor, "1");
        design.Wire(resistor, "2", led, "A");
        LinkChip(design, led, "K", chip, "3");

        var simulation = Start(design);
        Assert.False(simulation.HasHalted);
        var driver = simulation.Circuit.Average!.Branches.Single(branch => branch.ComponentId == chip.Id && branch.PinA == "3");
        Assert.InRange(driver.Current!.Value, minimum, maximum);
        var warnings = simulation.State.Diagnostics.Where(d => d.Code == "CHIP_OUTPUT_CURRENT").ToArray();
        Assert.Equal(overloaded ? 1 : 0, warnings.Length);
        if (overloaded) Assert.Contains("втекающий", warnings[0].Message);
    }

    [Fact]
    public void LedToGroundIsSourcedAndUnpoweredOutputRemainsZ()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var led = design.Add("led");
        var resistor = design.Add("resistor");
        var chip = Add(design, ChipTruthTableTests.Nand());
        LinkChip(design, uno, "GND", chip, "4");
        LinkChip(design, uno, "GND", chip, "1");
        LinkChip(design, uno, "GND", chip, "2");
        LinkChip(design, resistor, "1", chip, "3");
        design.Wire(resistor, "2", led, "A");
        design.Wire(led, "K", uno, "GND");
        var output = new PinReference(chip.Id, "3");
        Assert.Equal("CHIP_UNPOWERED", Assert.Single(design.Check(new ChipSupplyRule())).Code);
        var disconnected = Start(design);
        Assert.Equal(DigitalLevel.Z, disconnected.Circuit.ChipOutput(output));
        Assert.DoesNotContain(disconnected.Circuit.Average!.Branches, branch => branch.ComponentId == chip.Id);
        LinkChip(design, uno, "5V", chip, "8");
        Assert.Empty(design.Check(new ChipSupplyRule()));
        var powered = Start(design);
        Assert.Contains(powered.State.Diagnostics, d => d.Code == "CHIP_OUTPUT_CURRENT" && d.Message.Contains("вытекающий"));
    }

    [Fact]
    public void GpioClockLatchesOnItsRisingEdgeAfterExactDeterministicDelay()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var definition = ChipTruthTableTests.Nand() with
        {
            PropagationDelayUs = 200,
            Behavior = new ChipGraph([new(1, ChipGateKind.D, [new(Pin: 1), new(Pin: 2)])], [new(3, new(Gate: 1))])
        };
        var chip = Add(design, definition);
        LinkChip(design, uno, "5V", chip, "8");
        LinkChip(design, uno, "GND", chip, "4");
        LinkChip(design, uno, "D8", chip, "1");
        LinkChip(design, uno, "D9", chip, "2");
        var data = new PinReference(uno.Model.Id, "D8");
        var clock = new PinReference(uno.Model.Id, "D9");
        var output = new PinReference(chip.Id, "3");
        DigitalLevel[] Run()
        {
            var simulation = Start(design);
            simulation.SetPin(data, new(GpioMode.Output, true));
            simulation.SetPin(clock, new(GpioMode.Output));
            simulation.Schedule(500, () => simulation.SetPin(clock, new(GpioMode.Output, true)));
            simulation.Schedule(600, () => simulation.SetPin(data, new(GpioMode.Output)));
            simulation.Schedule(800, () => simulation.SetPin(clock, new(GpioMode.Output)));
            List<DigitalLevel> trace = [];
            foreach (var time in new long[] { 300, 699, 701, 850 })
                simulation.Schedule(time, () => trace.Add(simulation.Circuit.ChipOutput(output)));
            Assert.False(simulation.Step().IsHalted);
            return trace.ToArray();
        }
        var first = Run();
        Assert.Equal(new[] { DigitalLevel.X, DigitalLevel.X, DigitalLevel.High, DigitalLevel.High }, first);
        Assert.Equal(first, Run());
    }

    [Fact]
    public void IsolatedBenchHasIndependentClockOutputsAndTruthVerification()
    {
        var chip = ChipTruthTableTests.Nand() with { PropagationDelayUs = 250 };
        var factory = new ChipTestBenchFactory(new ChipEvaluator());
        var a = factory.Create(chip);
        var b = factory.Create(chip);
        a.SetInput(1, ChipLevel.High);
        a.SetInput(2, ChipLevel.High);
        Assert.Equal(ChipLevel.Low, a.Outputs[3]);
        Assert.Equal(ChipLevel.High, b.Outputs[3]);
        Assert.True(a.Time > b.Time);
        Assert.All(a.Trace(0).History, sample => Assert.Equal(ChipTestBench.ChannelCount, sample.Levels.Length));
        Assert.Empty(ChipBenchVerification.Verify(chip, (ChipTruthTable)chip.Behavior, factory));
    }

    [Fact]
    public void InputPulseShorterThanPropagationDelayDoesNotReachOutput()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var chip = Add(design, ChipTruthTableTests.Nand() with { PropagationDelayUs = 200 });
        LinkChip(design, uno, "5V", chip, "8");
        LinkChip(design, uno, "GND", chip, "4");
        LinkChip(design, uno, "D9", chip, "1");
        LinkChip(design, uno, "5V", chip, "2");
        var session = Start(design);
        var control = new PinReference(uno.Model.Id, "D9");
        var output = new PinReference(chip.Id, "3");
        session.SetPin(control, new(GpioMode.Output));
        session.Schedule(350, () => session.SetPin(control, new(GpioMode.Output, true)));
        session.Schedule(400, () => session.SetPin(control, new(GpioMode.Output)));
        var observed = new List<DigitalLevel>();
        foreach (var time in new long[] { 300, 380, 551, 601 })
            session.Schedule(time, () => observed.Add(session.Circuit.ChipOutput(output)));
        Assert.False(session.Step().IsHalted);
        Assert.Equal(4, observed.Count);
        Assert.All(observed, level => Assert.Equal(DigitalLevel.High, level));
    }


    private static Component Add(ElectricalTestCircuit design, ChipDefinition chip)
    {
        design.Project.Model.RegisterChip(chip);
        return design.Project.Model.AddComponent(ChipPackage.Component(chip, ChipTruthTableTests.Families[chip.Family]));
    }

    private static void LinkChip(ElectricalTestCircuit design, CircuitEditor.App.ViewModels.ComponentViewModel other,
        string otherPin, Component chip, string pin)
    {
        var contact = other.Footprint.Contacts.First(item => item.ElectricalPinId == otherPin);
        design.Project.Model.Circuit.PutWire(new Wire(WireEndpoint.Contact(other.Model.Id, contact.Id), WireEndpoint.Contact(chip.Id, pin)));
    }

    private static SimulationSession Start(ElectricalTestCircuit design)
    {
        var electrical = new JsonElectricalLibraryRepository();
        return new SimulationSessionFactory(new NetlistBuilder(electrical), new DcSolver(electrical),
            new JsonSimulationModelRepository(), new ScenarioRunner(new JsonScenarioRepository()),
            [new ChipBehavior(new JsonLogicFamilyRepository(), new ChipEvaluator()), new LedBehavior()])
            .Create(design.Project.Model);
    }
}
