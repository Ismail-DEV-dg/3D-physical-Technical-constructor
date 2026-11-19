namespace CircuitEditor.Core.Chips;

/// <summary>Независимые комбинационные секции с общей нумерацией выводов.</summary>
public sealed record ChipTruthTable(IReadOnlyList<ChipTruthSection> Sections) : ChipBehaviorDefinition;
