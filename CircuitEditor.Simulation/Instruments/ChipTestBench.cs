using System.Collections.Immutable;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Instruments;

public sealed class ChipTestBench
{
    private const int MaximumSamples = 256;
    private readonly ChipDefinition definition;
    private readonly ISimulationEngine timeline;
    private readonly ChipRuntime runtime;
    private readonly Dictionary<int, ChipLevel> inputs;
    private readonly Queue<TraceSample> samples = new();
    public const int ChannelCount = 8;

    internal ChipTestBench(ChipDefinition definition, IChipEvaluator evaluator, ISimulationEngine timeline)
    {
        this.definition = definition;
        this.timeline = timeline;
        runtime = new(definition, evaluator);
        inputs = definition.Pins.Where(pin => pin.Reads).ToDictionary(pin => pin.Number, _ => ChipLevel.Low);
        Channels = definition.Pins.Where(pin => pin.Reads || pin.Drives).OrderBy(pin => pin.Number).ToArray();
        Sample();
        EvaluateAndAdvance();
    }

    public IReadOnlyList<ChipPin> Channels { get; }
    public IReadOnlyDictionary<int, ChipLevel> Inputs => inputs;
    public IReadOnlyDictionary<int, ChipLevel> Outputs => runtime.Outputs;
    public long Time => timeline.Clock.Microseconds;
    public InstrumentSignal Trace(int firstChannel) => new(History: samples.Select(sample => sample with
    {
        Levels = sample.Levels.Skip(firstChannel).Concat(Enumerable.Repeat<string?>(null, ChannelCount))
            .Take(ChannelCount).ToImmutableArray()
    }).ToImmutableArray());

    public void SetInput(int pin, ChipLevel value)
    {
        if (!inputs.ContainsKey(pin)) return;
        inputs[pin] = value;
        EvaluateAndAdvance();
    }

    public void Pulse(int pin)
    {
        SetInput(pin, ChipLevel.Low);
        SetInput(pin, ChipLevel.High);
        SetInput(pin, ChipLevel.Low);
    }

    internal void SetInputs(IReadOnlyDictionary<int, ChipLevel> values)
    {
        foreach (var (pin, level) in values)
            if (inputs.ContainsKey(pin)) inputs[pin] = level;
        EvaluateAndAdvance();
    }

    private void EvaluateAndAdvance()
    {
        runtime.Apply(true, new Dictionary<int, ChipLevel>(inputs), Time, Schedule, _ => Sample());
        Sample();
        var end = Time + Math.Max(definition.PropagationDelayUs, SimulationEngine.StepMicroseconds);
        while (Time < end && !timeline.State.IsHalted) timeline.Step();
        Sample();
    }

    private void Schedule(long due, Func<bool> update) => timeline.Schedule(due, () => update());

    private void Sample()
    {
        if (samples.Count == MaximumSamples) samples.Dequeue();
        samples.Enqueue(new(Time, [], Channels.Select(pin => (string?)ChipTruthLogic.Symbol(
            pin.Drives ? Outputs[pin.Number] : inputs[pin.Number]).ToString()).ToImmutableArray()));
    }
}
