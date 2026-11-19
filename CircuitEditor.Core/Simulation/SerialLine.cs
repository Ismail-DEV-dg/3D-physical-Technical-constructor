namespace CircuitEditor.Core.Simulation;

/// <summary>Строка вывода с устойчивым порядком и временем сеанса.</summary>
public sealed record SerialLine(long Sequence, long Microseconds, string Text);
