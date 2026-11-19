using CircuitEditor.Core.Chips;

namespace CircuitEditor.Simulation.Components;

internal sealed class ChipRuntime
{
    private readonly ChipDefinition definition;
    private readonly Func<IReadOnlyDictionary<int, ChipLevel>, ChipState, ChipEvaluation> evaluate;
    private readonly IReadOnlyDictionary<int, ChipLevel> inactive;
    private ChipState state = ChipState.Empty;
    private IReadOnlyDictionary<int, ChipLevel>? previousInputs;
    private IReadOnlyDictionary<int, ChipLevel> requested;
    private long revision;

    internal ChipRuntime(ChipDefinition definition, IChipEvaluator evaluator)
    {
        this.definition = definition;
        evaluate = evaluator.Compile(definition);
        inactive = definition.Pins.Where(pin => pin.Drives).ToDictionary(pin => pin.Number, _ => ChipLevel.Z);
        Outputs = requested = inactive;
    }

    internal IReadOnlyDictionary<int, ChipLevel> Outputs { get; private set; }
    internal bool Settled { get; private set; } = true;
    internal double Supply { get; set; }
    internal string GroundPin { get; set; } = "";
    internal string SupplyPin { get; set; } = "";

    internal bool Apply(bool powered, IReadOnlyDictionary<int, ChipLevel> inputs, long time,
        Action<long, Func<bool>> schedule, Action<IReadOnlyDictionary<int, ChipLevel>> publish)
    {
        if (!powered)
        {
            if (previousInputs is null) return false;
            previousInputs = null;
            state = ChipState.Empty;
            requested = inactive;
            Settled = true;
            revision++;
            return Set(inactive, publish);
        }
        if (previousInputs is not null && Same(previousInputs, inputs)) return false;
        previousInputs = inputs;
        var result = evaluate(inputs, state);
        state = result.State;
        Settled = result.Settled;
        if (Same(requested, result.Outputs)) return false;

        requested = result.Outputs;
        var pending = ++revision;
        if (Same(Outputs, requested)) return false;
        if (definition.PropagationDelayUs == 0) return Set(requested, publish);
        schedule(time + definition.PropagationDelayUs, () => revision == pending && Set(result.Outputs, publish));
        return false;
    }

    private bool Set(IReadOnlyDictionary<int, ChipLevel> outputs, Action<IReadOnlyDictionary<int, ChipLevel>> publish)
    {
        if (Same(outputs, Outputs)) return false;
        Outputs = outputs;
        publish(outputs);
        return true;
    }

    private static bool Same(IReadOnlyDictionary<int, ChipLevel> a, IReadOnlyDictionary<int, ChipLevel> b) =>
        a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var value) && pair.Value == value);
}
