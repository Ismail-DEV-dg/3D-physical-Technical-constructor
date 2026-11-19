using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Tests;

public sealed class SimulationGpioTests
{
    [Fact]
    public void PwmAveragesCurrentAndReusesDcAcrossEdges()
    {
        using var design = new ElectricalTestCircuit();
        var uno = design.Add("arduino-uno");
        var resistor = design.Add("resistor");
        design.Wire(uno, "D9", resistor, "1");
        design.Wire(resistor, "2", uno, "GND");
        var circuit = Prepare(design);
        var pin = new PinReference(uno.Model.Id, "D9");
        Assert.True(circuit.TrySetPin(pin, new(GpioMode.Output, true)));
        circuit.Refresh();
        var full = circuit.Average!.Branches.Single(b => b.ComponentId == resistor.Model.Id).Current!.Value;
        var matrices = circuit.MatrixBuildCount;

        Assert.True(circuit.TrySetPin(pin, new(GpioMode.Pwm, Duty: 0.25)));
        circuit.Refresh();
        Assert.Equal(full / 4, circuit.Average!.Branches.Single(b => b.ComponentId == resistor.Model.Id).Current!.Value, 8);
        Assert.Equal(matrices, circuit.MatrixBuildCount);
        var solves = circuit.SolveCount;
        Assert.Equal(DigitalLevel.High, circuit.ReadDigital(pin, 0));
        Assert.Equal(DigitalLevel.Low, circuit.ReadDigital(pin, 1_500));
        circuit.Refresh();
        Assert.Equal(solves, circuit.SolveCount);
    }

    [Fact]
    public void Esp32InputOnlyAndAdcResolutionFollowBoardProfile()
    {
        using var design = new ElectricalTestCircuit();
        var esp = design.Add("esp32-devkit");
        var uno = design.Add("arduino-uno");
        var circuit = Prepare(design);
        Assert.False(circuit.TrySetPin(new(esp.Model.Id, "GPIO34"), new(GpioMode.Output, true)));
        Assert.Equal(4095, circuit.Controllers[esp.Model.Id].ReadAdc("GPIO34", 3.3));
        Assert.Equal(1023, circuit.Controllers[uno.Model.Id].ReadAdc("A0", 5));
    }

    internal static SimulationCircuit Prepare(ElectricalTestCircuit design)
    {
        var electrical = new JsonElectricalLibraryRepository();
        return new(design.Project.Model, new NetlistBuilder(electrical), new DcSolver(electrical),
            new JsonSimulationModelRepository().Load().Value!);
    }
}
