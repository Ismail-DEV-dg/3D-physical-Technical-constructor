namespace CircuitEditor.Core.Chips;

/// <summary>Единственный драйвер физического выхода микросхемы.</summary>
public sealed record ChipOutputLink(int Pin, ChipSignal Source);
