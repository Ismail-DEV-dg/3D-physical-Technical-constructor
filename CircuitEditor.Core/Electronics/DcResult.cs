namespace CircuitEditor.Core.Electronics;
/// <summary>Численные результаты и явные ограничения; неизвестные напряжения равны null.</summary>
public sealed record DcResult(bool Success,IReadOnlyDictionary<string,double?> Voltages,IReadOnlyList<DcBranchResult> Branches,
    IReadOnlyList<string> Diagnostics,IReadOnlyList<Guid> UnsupportedComponents)
{
    /// <summary>Стабилизация кусочно-линейной модели достигла лимита.</summary>
    public bool IterationLimitReached { get; init; }
    /// <summary>Узел отсчёта для каждого рассчитанного напряжения.</summary>
    public IReadOnlyDictionary<string, string> References { get; init; } = new Dictionary<string, string>();
}
