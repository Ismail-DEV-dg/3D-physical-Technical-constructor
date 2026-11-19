using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Scenarios;

public interface IScenarioRunner
{
    SimulationDiagnostic? Start(ScenarioConfiguration configuration, SimulationCircuit circuit, BehaviorContext context,
        ISimulationEngine engine, ISimulationClock clock, Action<string> serial);
}
