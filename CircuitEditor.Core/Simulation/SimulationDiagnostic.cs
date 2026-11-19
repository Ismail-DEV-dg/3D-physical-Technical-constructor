namespace CircuitEditor.Core.Simulation;

/// <summary>Идентификатор проблемы, её источник, измерение и модельное время.</summary>
public sealed record SimulationDiagnostic(string Code, string Message, long Microseconds, Guid? ComponentId = null,
    string? PinId = null, double? Measured = null, double? Limit = null, bool IsError = false);
