namespace CircuitEditor.Core.Electronics;
/// <summary>Знак тока задан от A к B, мощность положительна при потреблении.</summary>
public sealed record DcBranchResult(Guid ComponentId,string Name,string A,string B,double Voltage,double? Current,double? Power);
