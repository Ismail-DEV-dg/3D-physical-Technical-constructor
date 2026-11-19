using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Scenarios;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Engine;

public sealed class SimulationSessionFactory(INetlistBuilder nets, IDcSolver solver, ISimulationModelRepository repository,
    IScenarioRunner scenarios, IEnumerable<IComponentBehavior> behaviors) : ISimulationSessionFactory
{
    public SimulationSession Create(CircuitProject project, ScenarioConfiguration? scenario = null)
    {
        var models = repository.Load();
        var clock = new SimulationClock();
        return new(new(project, nets, solver, models.Value ?? new(new Dictionary<string, SimulationBoardProfile>(),
            new Dictionary<string, IReadOnlyDictionary<string, double>>())), models, clock, new SimulationEngine(clock),
            behaviors, scenarios, scenario);
    }
}
