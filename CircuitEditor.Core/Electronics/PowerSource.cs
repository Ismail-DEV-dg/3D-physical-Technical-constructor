using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Electronics;
/// <summary>Конкретный источник экземпляра с известным напряжением.</summary>
public sealed record PowerSource(Guid ComponentId, PinReference Positive, PinReference Negative, double Voltage);
