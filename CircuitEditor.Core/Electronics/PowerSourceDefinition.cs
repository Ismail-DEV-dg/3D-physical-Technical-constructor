namespace CircuitEditor.Core.Electronics;
/// <summary>Известный источник относительно собственного обратного вывода.</summary>
public sealed record PowerSourceDefinition(string Pin, string Ground, double Voltage, bool FromBatteryProperty = false);
