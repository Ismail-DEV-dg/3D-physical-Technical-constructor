namespace CircuitEditor.Core.Validation;
/// <summary>Источники разных напряжений, включая 5V и 3V3 одной платы.</summary>
public sealed class SourceConflictRule : IErcRule
{
    private const double VoltageTolerance=1e-6;
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c)
    {
        foreach(var group in c.Sources.GroupBy(s=>c.Nets.Find(s.ComponentId,s.Positive.PinId)?.Id).Where(g=>g.Key is not null))
            if(group.Max(s=>s.Voltage)-group.Min(s=>s.Voltage)>VoltageTolerance)
                yield return ErcContext.Issue("SOURCE_CONFLICT","Источники разных напряжений в одном узле.",group.Select(s=>s.Positive),[group.Key!]);
    }
}
