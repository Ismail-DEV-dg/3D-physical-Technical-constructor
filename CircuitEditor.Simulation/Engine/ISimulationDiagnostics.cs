using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Engine;

public interface ISimulationDiagnostics
{
    IEnumerable<SimulationDiagnostic> Read(SimulationCircuit circuit, SimulationModelCatalog catalog, long now);
    SimulationDiagnostic InvalidPin(SimulationCircuit circuit, PinReference pin, GpioState requested, long now);
}
