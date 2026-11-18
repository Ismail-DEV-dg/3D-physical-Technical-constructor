namespace CircuitEditor.Core.Electronics;
/// <summary>Подобранный номинал, фактический ток и мощность резистора.</summary>
public sealed record LedResistorResult(double Resistance,double Current,double Power,double Vf);
