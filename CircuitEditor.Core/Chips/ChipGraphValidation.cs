namespace CircuitEditor.Core.Chips;

/// <summary>Проверка проводов, ограничений и порядка комбинационных блоков.</summary>
public static class ChipGraphValidation
{
    /// <summary>Максимум логических блоков в одной микросхеме.</summary>
    public const int MaximumGates = 128;
    private const int MaximumDepth = 48;
    private const int MaximumFanIn = 8;

    internal static void Validate(ChipGraph graph, IReadOnlySet<int> inputs, IReadOnlySet<int> outputs,
        IEnumerable<int> enablePins, List<string> errors)
    {
        if (graph.Gates is null || graph.Outputs is null || graph.Gates.Count > MaximumGates
            || graph.Gates.Any(gate => gate is null || gate.Id < 1 || !Enum.IsDefined(gate.Kind))
            || graph.Gates.Select(gate => gate.Id).Distinct().Count() != graph.Gates.Count)
        {
            errors.Add($"Проверьте идентификаторы и типы блоков. Максимум блоков: {MaximumGates}.");
            return;
        }
        var usedInputs = enablePins.ToHashSet();
        var gates = graph.Gates.Select(gate => gate.Id).ToHashSet();
        foreach (var gate in graph.Gates)
        {
            if (gate.Inputs is null || !ValidArity(gate.Kind, gate.Inputs.Count)
                || gate.Kind is not (ChipGateKind.D or ChipGateKind.Jk) && (gate.Clear is not null || gate.Preset is not null)
                || Signals(gate).Any(signal => !ValidWire(signal, inputs, gates)))
            {
                errors.Add($"Блок {gate.Id}: проверьте число портов и провода.");
                continue;
            }
            CountInputs(Signals(gate), usedInputs);
        }

        if (graph.Outputs.Any(output => output is null || !outputs.Contains(output.Pin)
                || !ValidWire(output.Source, inputs, gates))
            || graph.Outputs.Select(output => output.Pin).Distinct().Count() != graph.Outputs.Count
            || graph.Outputs.Count != outputs.Count)
            errors.Add("К каждому выходу подключите ровно один драйвер.");
        else
            CountInputs(graph.Outputs.Select(output => output.Source), usedInputs);
        if (!inputs.SetEquals(usedInputs)) errors.Add("Есть входы, не привязанные к поведению.");

        try { Ordered(graph); }
        catch (ArgumentException error) { errors.Add(error.Message); }
    }

    internal static IReadOnlyList<ChipGate> Ordered(ChipGraph graph)
    {
        var remaining = graph.Gates.Where(gate => !gate.HasMemory).ToDictionary(gate => gate.Id);
        Dictionary<int, int> depths = [];
        List<ChipGate> result = [];
        while (remaining.Count > 0)
        {
            var ready = remaining.Values.Where(gate =>
                (gate.Inputs ?? []).All(source => source?.Gate is not int id || !remaining.ContainsKey(id))).ToArray();
            if (ready.Length == 0) throw new ArgumentException("Найдена комбинационная петля без блока памяти.");
            foreach (var gate in ready)
            {
                var depth = 1 + (gate.Inputs ?? []).Select(source => source?.Gate is int id
                    ? depths.GetValueOrDefault(id) : 0).DefaultIfEmpty().Max();
                if (depth > MaximumDepth) throw new ArgumentException($"Глубина схемы превышает {MaximumDepth} блоков.");
                depths[gate.Id] = depth;
                result.Add(gate);
                remaining.Remove(gate.Id);
            }
        }
        return result;
    }

    private static IEnumerable<ChipSignal> Signals(ChipGate gate) =>
        gate.Inputs.Concat(new[] { gate.Clear, gate.Preset }.OfType<ChipSignal>());

    private static bool ValidWire(ChipSignal? signal, IReadOnlySet<int> inputs, IReadOnlySet<int> gates) =>
        signal switch
        {
            { Pin: int pin, Gate: null, Constant: null } => inputs.Contains(pin),
            { Gate: int gate, Pin: null, Constant: null } => gates.Contains(gate),
            { Constant: ChipLevel value, Gate: null, Pin: null } => Enum.IsDefined(value),
            _ => false
        };

    private static void CountInputs(IEnumerable<ChipSignal> signals, HashSet<int> used) =>
        used.UnionWith(signals.Where(signal => signal.Pin.HasValue).Select(signal => signal.Pin!.Value));

    private static bool ValidArity(ChipGateKind kind, int count) => kind switch
    {
        ChipGateKind.Not => count == 1,
        ChipGateKind.D or ChipGateKind.Rs => count == 2,
        ChipGateKind.Jk => count == 3,
        _ => count is >= 2 and <= MaximumFanIn
    };
}
