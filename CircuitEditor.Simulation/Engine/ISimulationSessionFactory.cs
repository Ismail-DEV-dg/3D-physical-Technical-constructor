using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Engine;

public interface ISimulationSessionFactory
{
    SimulationSession Create(CircuitProject project, ScenarioConfiguration? scenario = null);
}
