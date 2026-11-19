using System.Collections.Immutable;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Gpio;
using CircuitEditor.Simulation.Scenarios;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Engine;

public sealed partial class SimulationSession
{
    private const int IterationLimit = 32;
    private const int SerialBufferLimit = 512;
    private readonly ISimulationClock clock;
    private readonly ISimulationEngine timeline;
    private readonly BehaviorContext context;
    private readonly (Component Part, IComponentBehavior Behavior)[] parts;
    private readonly IReadOnlySet<Guid> modeled;
    private readonly List<SimulationDiagnostic> diagnostics = [];
    private readonly HashSet<(string Code, Guid? Component, string? Pin)> diagnosticKeys = [];
    private readonly Queue<string> serial = [];
    private long observedTime;
    private string? failure;

    internal SimulationSession(SimulationCircuit circuit, RepositoryResult<SimulationModelCatalog> models, ISimulationClock clock,
        ISimulationEngine timeline, IEnumerable<IComponentBehavior> behaviors, IScenarioRunner scenarios, ScenarioConfiguration? selection)
    {
        Circuit = circuit;
        this.clock = clock;
        this.timeline = timeline;
        var registry = behaviors.SelectMany(b => b.ComponentTypes.Select(type => (type, b))).ToDictionary(pair => pair.type, pair => pair.b);
        parts = circuit.Project.Circuit.Components.Where(c => registry.ContainsKey(c.Definition.Id))
            .Select(c => (c, registry[c.Definition.Id])).ToArray();
        modeled = parts.Select(p => p.Part.Id).ToHashSet();
        context = new(circuit, models.Value ?? new(new Dictionary<string, SimulationBoardProfile>(),
            new Dictionary<string, IReadOnlyDictionary<string, double>>()), Schedule);
        if (models.Value is null)
            Halt(new("SIM_LIBRARY", models.Error ?? "Нет параметров симуляции.", 0, IsError: true));
        else
        {
            foreach (var part in circuit.Project.Circuit.Components.Where(c => c.Definition.Id is "capacitor" or "inductor"))
                Report(new("SIM_UNSUPPORTED", $"{part.Reference}: модель не поддерживается во времени.", 0, part.Id));
            if (selection is not null && scenarios.Start(selection, circuit, context, new SessionTimeline(this), clock, AppendSerial) is { } invalid)
                Halt(invalid);
        }
        Settle();
        State = Capture();
    }

    public SimulationCircuit Circuit { get; }
    public SimulationState State { get; private set; }

    public SimulationState Step(CancellationToken token = default)
    {
        Advance(token);
        return Observe();
    }

    public bool HasHalted => failure is not null;

    public void Advance(CancellationToken token = default)
    {
        if (HasHalted) return;
        var step = timeline.Step(token);
        if (step.IsHalted)
            Halt(new("SIM_OSCILLATION", step.Failure!, clock.Microseconds, IsError: true));
        Settle();
    }

    public SimulationState Observe()
    {
        Settle();
        return State = Capture();
    }

    public void Schedule(long microseconds, Action action) => timeline.Schedule(microseconds, () =>
    {
        if (failure is not null) return;
        Settle();
        action();
        Settle();
    });

    public bool SetPin(PinReference pin, GpioState state) => Circuit.TrySetPin(pin, state);

    public bool SetInput(Guid id, ComponentInput input, double value)
    {
        var component = Circuit.Project.Circuit.Components.FirstOrDefault(c => c.Id == id);
        if (component is null || input == ComponentInput.RelayEnergized || !component.TrySetInput(input, value)) return false;
        if (input is ComponentInput.ButtonPressed or ComponentInput.SwitchPosition) Circuit.RebuildTopology();
        else Circuit.InvalidateParameters();
        return true;
    }

    public bool SetPotentiometer(Guid id, double angle)
    {
        var component = Circuit.Project.Circuit.Components.FirstOrDefault(c => c.Id == id);
        if (component?.TrySetPotentiometerAngle(angle) != true) return false;
        Circuit.InvalidateParameters();
        return true;
    }

    public string[] DrainSerial()
    {
        var lines = serial.ToArray();
        serial.Clear();
        return lines;
    }

    private void AppendSerial(string line)
    {
        if (serial.Count == SerialBufferLimit) serial.Dequeue();
        serial.Enqueue(line);
    }

    private void Settle()
    {
        context.Now = clock.Microseconds;
        if (failure is not null) return;
        for (var i = 0; i < IterationLimit; i++)
        {
            Circuit.Refresh(() => parts.SelectMany(p => p.Behavior.Branches(context, p.Part)).ToArray(), modeled);
            if (Circuit.IterationLimitReached) break;
            var topology = false;
            foreach (var (part, behavior) in parts)
            {
                if (!behavior.Update(context, part)) continue;
                if (part.Definition.Id == "relay") topology = true;
                else Circuit.InvalidateParameters();
            }
            if (topology) Circuit.RebuildTopology();
            if (!Circuit.IsDirty) return;
        }
        Halt(new("SIM_OSCILLATION", "Колебательный режим или зацикливание: электрические состояния не стабилизировались.",
            clock.Microseconds, IsError: true));
    }

    private SimulationState Capture()
    {
        context.Elapsed = context.Now - observedTime;
        observedTime = context.Now;
        var visuals = parts.Select(p => (p.Part.Id, Value: p.Behavior.Observe(context, p.Part)))
            .Where(p => p.Value is not null).ToImmutableDictionary(p => p.Id, p => p.Value!);
        return new(clock.Microseconds, failure is not null, failure)
        {
            Components = visuals,
            Wires = Circuit.Wires(),
            Pins = ReadPins(),
            Diagnostics = diagnostics.ToImmutableArray()
        };
    }

    private void Halt(SimulationDiagnostic diagnostic)
    {
        Report(diagnostic);
        failure = diagnostic.Message;
    }

    private void Report(SimulationDiagnostic diagnostic)
    {
        if (diagnosticKeys.Add((diagnostic.Code, diagnostic.ComponentId, diagnostic.PinId))) diagnostics.Add(diagnostic);
    }
}
