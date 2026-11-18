namespace CircuitEditor.Core.Electronics;
/// <summary>Численные результаты и явные ограничения; неизвестные напряжения равны null.</summary>
public sealed record DcResult(bool Success,IReadOnlyDictionary<string,double?> Voltages,IReadOnlyList<DcBranchResult> Branches,
    IReadOnlyList<string> Diagnostics,IReadOnlyList<Guid> UnsupportedComponents);
