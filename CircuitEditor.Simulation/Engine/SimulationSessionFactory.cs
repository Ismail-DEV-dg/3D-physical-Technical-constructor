using CircuitEditor.Core.Code;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Instruments;
using CircuitEditor.Simulation.Scenarios;
using CircuitEditor.Simulation.Timing;
using CircuitEditor.Simulation.Sketches;

namespace CircuitEditor.Simulation.Engine;

public sealed class SimulationSessionFactory(INetlistBuilder nets, IDcSolver solver, ISimulationModelRepository repository,
    IScenarioRunner scenarios, IEnumerable<IComponentBehavior> behaviors, ISimulationDiagnostics? diagnostics = null,
    IEnumerable<IInstrument>? instruments = null, Func<ISimulationEngine>? createTimeline = null, ISketchRunner? sketches = null) : ISimulationSessionFactory
{
    public SimulationSession Create(CircuitProject project, ScenarioConfiguration? scenario = null)
    {
        var models = repository.Load();
        var timeline = createTimeline?.Invoke() ?? new SimulationEngine(new SimulationClock());
        var catalog = models.Value ?? new(new Dictionary<string, SimulationBoardProfile>(),
            new Dictionary<string, IReadOnlyDictionary<string, double>>());
        return new(new(project, nets, solver, catalog), catalog, models.Error, timeline.Clock, timeline,
            behaviors, scenarios, scenario, diagnostics ?? new SimulationDiagnostics(),
            instruments ?? [new Multimeter(), new Oscilloscope(), new LogicAnalyzer()]);
    }

    public SimulationSession CreateForSketch(CircuitProject project, SketchDocument sketch, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = Create(project);
        if (session.HasHalted) return session;
        var error = sketches is null
            ? new CodeProblem("Режим запуска скетча недоступен.", Severity: "Error", Code: "CODE_RUN")
            : sketches.Start(sketch, session, cancellationToken);
        if (error is not null)
        {
            session.RejectSketch(error);
            session.Observe();
        }
        else session.Step(cancellationToken);
        return session;
    }
}
