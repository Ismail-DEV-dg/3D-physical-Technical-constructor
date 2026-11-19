namespace CircuitEditor.Core.Simulation;

/// <summary>Напряжение перемычки и ток от её начала к концу, если он однозначен.</summary>
public sealed record WireSignal(double? Voltage, double? Current);
