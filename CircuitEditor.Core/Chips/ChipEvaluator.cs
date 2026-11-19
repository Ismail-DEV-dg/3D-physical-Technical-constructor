using System.Collections.ObjectModel;

namespace CircuitEditor.Core.Chips;

/// <summary>Компилирует цифровые определения и рассчитывает выходы с учётом разрешения драйверов.</summary>
public sealed class ChipEvaluator : IChipEvaluator
{
    private const int SettlementPasses = 64;

    /// <inheritdoc/>
    public Func<IReadOnlyDictionary<int, ChipLevel>, ChipState, ChipEvaluation> Compile(ChipDefinition definition)
    {
        var raw = definition.Behavior switch
        {
            ChipTruthTable table => (inputs, state) => new ChipEvaluation(ChipTruthLogic.Evaluate(table, inputs), state),
            ChipGraph graph => CompileGraph(graph),
            _ => throw new ArgumentException("Неизвестное поведение микросхемы.", nameof(definition))
        };
        var outputs = definition.Pins.Where(pin => pin.Drives).ToArray();
        return (inputs, state) =>
        {
            var result = raw(inputs, state);
            return result with { Outputs = ReadOnly(outputs.ToDictionary(pin => pin.Number,
                pin => Drive(pin, result.Outputs.GetValueOrDefault(pin.Number, ChipLevel.X), inputs))) };
        };
    }

    private static Func<IReadOnlyDictionary<int, ChipLevel>, ChipState, ChipEvaluation> CompileGraph(ChipGraph graph)
    {
        var ordered = ChipGraphValidation.Ordered(graph);
        var storage = graph.Gates.Where(gate => gate.HasMemory).ToArray();
        return (supplied, state) =>
        {
            var memory = storage.ToDictionary(gate => gate.Id, gate => state.Memory.GetValueOrDefault(gate.Id, ChipLevel.X));
            var clocks = new Dictionary<int, ChipLevel>(state.Clocks);
            var levels = new Dictionary<int, ChipLevel>();
            ChipLevel Read(ChipSignal source) => source.Pin is int pin ? supplied.GetValueOrDefault(pin, ChipLevel.Z)
                : source.Gate is int gate ? levels[gate] : source.Constant!.Value;

            for (var pass = 0; pass < SettlementPasses; pass++)
            {
                foreach (var (gate, level) in memory) levels[gate] = level;
                foreach (var gate in ordered)
                    levels[gate.Id] = ChipGateLogic.Combine(gate.Kind, gate.Inputs.Select(Read).ToArray());

                // Все блоки памяти считывают один снимок. При совпадении фронтов защёлка
                // получает предыдущее состояние регистра, даже если тот одновременно сдвигается.
                var changed = false;
                foreach (var gate in storage)
                {
                    var ports = gate.Inputs.Select(Read).ToArray();
                    var edge = false;
                    if (gate.Kind != ChipGateKind.Rs)
                    {
                        var clock = ports[^1];
                        edge = clock == ChipLevel.High && clocks.GetValueOrDefault(gate.Id, ChipLevel.X) == ChipLevel.Low;
                        clocks[gate.Id] = clock;
                    }
                    var next = ChipGateLogic.Store(gate.Kind, ports, memory[gate.Id], edge,
                        gate.Clear is null ? ChipLevel.Low : Read(gate.Clear),
                        gate.Preset is null ? ChipLevel.Low : Read(gate.Preset));
                    changed |= memory[gate.Id] != next;
                    memory[gate.Id] = next;
                }
                if (!changed)
                    return new(ReadOnly(graph.Outputs.ToDictionary(link => link.Pin, link => Read(link.Source))),
                        new(ReadOnly(memory), ReadOnly(clocks)));
            }

            return new(ReadOnly(graph.Outputs.ToDictionary(link => link.Pin, _ => ChipLevel.X)), ChipState.Empty, false);
        };
    }

    private static ChipLevel Drive(ChipPin pin, ChipLevel value, IReadOnlyDictionary<int, ChipLevel> inputs)
    {
        if (pin.EnablePin is int control)
        {
            var enable = inputs.GetValueOrDefault(control, ChipLevel.Z);
            if (enable is ChipLevel.X or ChipLevel.Z) return ChipLevel.X;
            if ((enable == ChipLevel.Low) != pin.EnableLow) return ChipLevel.Z;
        }
        return pin.Type == ChipPinType.OpenCollector && value == ChipLevel.High ? ChipLevel.Z : value;
    }

    private static IReadOnlyDictionary<int, ChipLevel> ReadOnly(Dictionary<int, ChipLevel> values) =>
        new ReadOnlyDictionary<int, ChipLevel>(values);
}
