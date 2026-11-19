using System.Collections.Immutable;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Simulation;

/// <summary>Неизменяемый снимок модельного времени и состояния сеанса.</summary>
public sealed record SimulationState(long Microseconds = 0, bool IsHalted = false, string? Failure = null)
{
    /// <summary>Визуальные выходы по экземплярам компонентов.</summary>
    public ImmutableDictionary<Guid, ComponentSignal> Components { get; init; } = ImmutableDictionary<Guid, ComponentSignal>.Empty;
    /// <summary>Измерения выводов платы.</summary>
    public ImmutableDictionary<PinReference, PinSignal> Pins { get; init; } = ImmutableDictionary<PinReference, PinSignal>.Empty;
    /// <summary>Напряжение и ток перемычек текущего расчёта.</summary>
    public ImmutableDictionary<Guid, WireSignal> Wires { get; init; } = ImmutableDictionary<Guid, WireSignal>.Empty;
    /// <summary>Диагностика с момента запуска, без повторений.</summary>
    public ImmutableArray<SimulationDiagnostic> Diagnostics { get; init; } = [];
}
