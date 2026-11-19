namespace CircuitEditor.Core.Simulation;

/// <summary>Неизменяемый снимок модельного времени и состояния сеанса.</summary>
public sealed record SimulationState(long Microseconds = 0, bool IsHalted = false, string? Failure = null);
