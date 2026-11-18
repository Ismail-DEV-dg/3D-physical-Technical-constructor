namespace CircuitEditor.Core.Electronics;
/// <summary>Подготовленные поддерживаемые ветви.</summary>
public sealed record DcAssembly(IReadOnlyList<DcBranch> Branches,IReadOnlyList<Guid> Unsupported,IReadOnlyList<string> Diagnostics,bool HasErrors=false);
