namespace CircuitEditor.Core.Chips;

/// <summary>Граф логических блоков с привязками выходов к номерам выводов DIP.</summary>
public sealed record ChipGraph(IReadOnlyList<ChipGate> Gates, IReadOnlyList<ChipOutputLink> Outputs) : ChipBehaviorDefinition;
