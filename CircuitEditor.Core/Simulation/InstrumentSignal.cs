using System.Collections.Immutable;

namespace CircuitEditor.Core.Simulation;

/// <summary>Показание мультиметра или ограниченная история сигнала.</summary>
public sealed record InstrumentSignal(double? Volts = null, double? Amps = null, ImmutableArray<TraceSample> History = default);
