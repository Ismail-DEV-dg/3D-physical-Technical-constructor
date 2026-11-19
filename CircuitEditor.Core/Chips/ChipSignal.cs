namespace CircuitEditor.Core.Chips;

/// <summary>Один источник провода: вывод, выход блока или постоянный уровень.</summary>
public sealed record ChipSignal(int? Pin = null, int? Gate = null, ChipLevel? Constant = null);
