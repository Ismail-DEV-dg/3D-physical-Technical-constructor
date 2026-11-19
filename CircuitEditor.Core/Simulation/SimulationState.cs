using System.Collections.Immutable;

namespace CircuitEditor.Core.Simulation;

/// <summary>Неизменяемый снимок модельного времени и состояния сеанса.</summary>
public sealed record SimulationState(long Microseconds = 0, bool IsHalted = false, string? Failure = null)
{
    /// <summary>Визуальные выходы по экземплярам компонентов.</summary>
    public ImmutableDictionary<Guid, ComponentSignal> Components { get; init; } = ImmutableDictionary<Guid, ComponentSignal>.Empty;
    /// <summary>Диагностика с момента запуска, без повторений.</summary>
    public ImmutableArray<SimulationDiagnostic> Diagnostics { get; init; } = [];
}
