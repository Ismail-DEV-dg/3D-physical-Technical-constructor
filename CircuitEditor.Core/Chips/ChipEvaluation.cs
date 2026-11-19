namespace CircuitEditor.Core.Chips;

/// <summary>Новые выходы и память экземпляра после применения входов.</summary>
public sealed record ChipEvaluation(IReadOnlyDictionary<int, ChipLevel> Outputs, ChipState State, bool Settled = true);
