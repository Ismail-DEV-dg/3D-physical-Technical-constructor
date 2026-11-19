namespace CircuitEditor.Core.Simulation;

/// <summary>Уровень, аналоговый отсчёт и напряжение вывода в момент снимка.</summary>
public sealed record PinSignal(string Level, int? Adc, double? Voltage);
