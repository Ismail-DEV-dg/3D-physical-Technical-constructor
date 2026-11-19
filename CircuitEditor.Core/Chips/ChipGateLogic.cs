namespace CircuitEditor.Core.Chips;

/// <summary>Четырёхзначная логика комбинационных элементов и триггеров.</summary>
public static class ChipGateLogic
{
    private const ChipLevel Off = ChipLevel.Low;
    private const ChipLevel On = ChipLevel.High;
    private const ChipLevel Unknown = ChipLevel.X;

    internal static ChipLevel Combine(ChipGateKind kind, IReadOnlyList<ChipLevel> levels) => kind switch
    {
        ChipGateKind.And => And(levels),
        ChipGateKind.Or => Or(levels),
        ChipGateKind.Not => Invert(levels[0]),
        ChipGateKind.Nand => Invert(And(levels)),
        ChipGateKind.Nor => Invert(Or(levels)),
        ChipGateKind.Xor => Xor(levels),
        ChipGateKind.Xnor => Invert(Xor(levels)),
        _ => Unknown
    };

    internal static ChipLevel Store(ChipGateKind kind, IReadOnlyList<ChipLevel> signals,
        ChipLevel previous, bool edge, ChipLevel clear, ChipLevel preset)
    {
        if (kind == ChipGateKind.Rs) return Sr(signals[0], signals[1], previous);
        var requested = !edge ? previous : kind == ChipGateKind.D ? Known(signals[0])
            : Jk(signals[0], signals[1], previous);
        return Sr(preset, clear, requested);
    }

    private static ChipLevel Sr(ChipLevel set, ChipLevel reset, ChipLevel previous) => (set, reset) switch
    {
        (On, Off) => On,
        (Off, On) => Off,
        (Off, Off) => previous,
        _ => Unknown
    };

    private static ChipLevel Jk(ChipLevel j, ChipLevel k, ChipLevel previous) => (j, k) switch
    {
        (On, Off) => On,
        (Off, On) => Off,
        (Off, Off) => previous,
        (On, On) => Invert(previous),
        _ => Unknown
    };

    private static ChipLevel And(IReadOnlyList<ChipLevel> values) =>
        values.Contains(Off) ? Off : values.All(value => value == On) ? On : Unknown;

    private static ChipLevel Or(IReadOnlyList<ChipLevel> values) =>
        values.Contains(On) ? On : values.All(value => value == Off) ? Off : Unknown;

    private static ChipLevel Xor(IReadOnlyList<ChipLevel> values) =>
        values.Any(value => value is not (Off or On)) ? Unknown : values.Count(value => value == On) % 2 == 1 ? On : Off;

    private static ChipLevel Invert(ChipLevel value) => value switch { Off => On, On => Off, _ => Unknown };
    private static ChipLevel Known(ChipLevel value) => value is Off or On ? value : Unknown;
}
