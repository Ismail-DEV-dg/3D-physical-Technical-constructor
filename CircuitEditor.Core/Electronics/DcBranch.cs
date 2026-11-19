namespace CircuitEditor.Core.Electronics;
/// <summary>Ветвь MNA: R, V или кусочно-линейный LED.</summary>
public sealed record DcBranch(Guid ComponentId,string Name,string A,string B,string Kind,double Value,double Vf=0,double MaximumCurrent=0, string? PinA=null, string? PinB=null);
