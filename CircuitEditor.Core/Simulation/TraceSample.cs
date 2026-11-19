using System.Collections.Immutable;

namespace CircuitEditor.Core.Simulation;

/// <summary>Мгновенный отсчёт всех каналов одного прибора.</summary>
public sealed record TraceSample(long Microseconds, ImmutableArray<double?> Voltages, ImmutableArray<string?> Levels);
