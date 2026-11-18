namespace CircuitEditor.Core.Electronics;
/// <summary>Идеальное внутреннее соединение, условие задаётся состоянием экземпляра.</summary>
public sealed record ElectricalLink(IReadOnlyList<string> Pins, string? Input = null, double Value = 0);
