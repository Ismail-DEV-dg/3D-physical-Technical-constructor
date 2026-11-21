using System.Collections.Immutable;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Gpio;
using CircuitEditor.Simulation.Instruments;
using CircuitEditor.Simulation.Scenarios;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Engine;

public sealed class SimulationSession
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
    private readonly Queue<SerialLine> serial = [];
    private readonly SessionInstruments instruments;
    private readonly ISimulationDiagnostics diagnosticReader;
    private readonly SimulationModelCatalog catalog;
    private long serialSequence;
    private string? failure;

    internal SimulationSession(SimulationCircuit circuit, SimulationModelCatalog catalog, string? libraryError,
        ISimulationClock clock, ISimulationEngine timeline, IEnumerable<IComponentBehavior> behaviors,
        IScenarioRunner scenarios, ScenarioConfiguration? selection, ISimulationDiagnostics diagnosticReader,
        IEnumerable<IInstrument> instruments)
    {
        Circuit = circuit;
        this.catalog = catalog;
        this.diagnosticReader = diagnosticReader;
        this.instruments = new(circuit, instruments);
        this.clock = clock;
        this.timeline = timeline;
        var registry = behaviors.ToDictionary(behavior => behavior.ComponentType);
        parts = circuit.Project.Circuit.Components.Select(component => (Part: component,
                Behavior: registry.GetValueOrDefault(component.Definition.Id) ??
                    (circuit.Project.FindChip(component.Definition.Id) is null ? null : registry.GetValueOrDefault(ChipBehavior.TypeId))))
            .Where(part => part.Behavior is not null).Select(part => (part.Part, part.Behavior!)).ToArray();
        modeled = parts.Select(p => p.Part.Id).ToHashSet();
        context = new(circuit, catalog, Schedule);
        if (libraryError is not null)
            Halt(new("SIM_LIBRARY", libraryError, 0, IsError: true));
        else
        {
            foreach (var part in circuit.Project.Circuit.Components.Where(c => c.Definition.Id is "capacitor" or "inductor"))
                Report(new("SIM_UNSUPPORTED", $"{part.Reference}: модель не поддерживается во времени.", 0, part.Id));
            if (selection is not null && scenarios.Start(selection, circuit, context, Schedule, clock, AppendSerial) is { } invalid)
                Halt(invalid);
        }
        Settle();
        State = Capture();
    }

    public SimulationCircuit Circuit { get; }
    public SimulationState State { get; private set; }
    public long ModelTime => clock.Microseconds;
    public bool HasHalted => failure is not null;

    public SimulationState Step(CancellationToken token = default)
    {
        Advance(token);
        return Observe();
    }

    public void Advance(CancellationToken token = default)
    {
        if (HasHalted) return;
        if (Circuit.IsDirty) Settle();
        if (HasHalted) return;
        if (instruments.IsRecording)
        {
            var now = clock.Microseconds;
            for (var time = now + SessionInstruments.SampleMicroseconds;
                time <= now + SimulationEngine.StepMicroseconds; time += SessionInstruments.SampleMicroseconds)
                timeline.Schedule(time, SampleInstruments);
        }
        var step = timeline.Step(token);
        if (step.IsHalted && !HasHalted)
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
        if (HasHalted) return;
        Settle();
        if (HasHalted) return;
        action();
        Settle();
    });

    internal void ScheduleSketchStep(long microseconds, Action run) => timeline.Schedule(microseconds, () =>
    {
        if (HasHalted) return;
        run();
        if (Circuit.IsDirty) Settle();
    });

    internal DigitalLevel ReadSketchPin(PinReference pin)
    {
        if (Circuit.IsDirty) Settle();
        return Circuit.ReadDigital(pin, ModelTime);
    }

    internal void RejectSketch(CircuitEditor.Core.Code.CodeProblem problem) =>
        Halt(new(problem.Code, problem.Message, ModelTime, IsError: true,
            SketchLine: problem.Line, SketchColumn: problem.Column));

    private void SampleInstruments()
    {
        if (Circuit.IsDirty) Settle();
        if (!HasHalted) instruments.Sample(clock.Microseconds);
    }

    public bool AssignProbe(InstrumentKind kind, int channel, WireEndpoint? endpoint) => instruments.Assign(kind, channel, endpoint);

    public bool SetPin(PinReference pin, GpioState state)
    {
        if (Circuit.TrySetPin(pin, state)) return true;
        Report(diagnosticReader.InvalidPin(Circuit, pin, state, clock.Microseconds));
        return false;
    }

    public bool SetInput(Guid id, ComponentInput input, double value)
    {
        var component = Circuit.Project.Circuit.Components.FirstOrDefault(c => c.Id == id);
        if (component is null || input == ComponentInput.RelayEnergized || !component.Inputs.Values.TryGetValue(input, out var previous))
            return false;
        if (previous == value) return true;
        if (!component.TrySetInput(input, value)) return false;
        if (input is ComponentInput.ButtonPressed or ComponentInput.SwitchPosition) Circuit.RebuildTopology();
        else if (component.Definition.Id is "photoresistor" or "thermistor" or "pir") Circuit.InvalidateParameters();
        return true;
    }

    public bool SetPotentiometer(Guid id, double angle)
    {
        var component = Circuit.Project.Circuit.Components.FirstOrDefault(c => c.Id == id);
        if (component is null) return false;
        if (component.PotentiometerAngle == angle) return component.Definition.Id == "potentiometer";
        if (!component.TrySetPotentiometerAngle(angle)) return false;
        Circuit.InvalidateParameters();
        return true;
    }

    private void AppendSerial(string line)
    {
        if (serial.Count == SerialBufferLimit) serial.Dequeue();
        serial.Enqueue(new(++serialSequence, clock.Microseconds, line));
    }

    private void Settle()
    {
        context.Now = clock.Microseconds;
        if (HasHalted) return;
        var changed = Circuit.IsDirty;
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
            if (!Circuit.IsDirty)
            {
                if (changed || i > 0)
                {
                    foreach (var problem in diagnosticReader.Read(Circuit, catalog, clock.Microseconds)) Report(problem);
                    foreach (var (part, behavior) in parts)
                        foreach (var problem in behavior.Diagnostics(context, part)) Report(problem);
                }
                return;
            }
        }
        Halt(new("SIM_OSCILLATION", "Колебательный режим или зацикливание: электрические состояния не стабилизировались.",
            clock.Microseconds, IsError: true));
    }

    private SimulationState Capture()
    {
        var visuals = parts.Select(p => (p.Part.Id, Value: p.Behavior.Observe(context, p.Part)))
            .Where(p => p.Value is not null).ToImmutableDictionary(p => p.Id, p => p.Value!);
        foreach (var board in Circuit.Controllers.Values)
            if (board.Profile.OnboardLedPin is { } pin)
            {
                var level = Circuit.ReadDigital(new PinReference(board.Id, pin), clock.Microseconds);
                visuals = visuals.SetItem(board.Id, new(Intensity: level == DigitalLevel.High ? 1 : 0));
            }
        return new(clock.Microseconds, HasHalted, failure)
        {
            Components = visuals,
            Wires = Circuit.Wires(),
            Pins = PinMeasurements.Read(Circuit, clock.Microseconds),
            Instruments = instruments.Capture(clock.Microseconds),
            Serial = serial.ToImmutableArray(),
            Diagnostics = diagnostics.ToImmutableArray()
        };
    }

    private void Halt(SimulationDiagnostic diagnostic)
    {
        Report(diagnostic);
        failure = diagnostic.Message;
        timeline.Halt(failure);
    }

    private void Report(SimulationDiagnostic diagnostic)
    {
        if (diagnosticKeys.Add((diagnostic.Code, diagnostic.ComponentId, diagnostic.PinId))) diagnostics.Add(diagnostic);
    }
}
