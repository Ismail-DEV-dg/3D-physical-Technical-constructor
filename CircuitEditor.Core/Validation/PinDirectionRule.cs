namespace CircuitEditor.Core.Validation;
/// <summary>Заглушка правил направления: этап 6–7, роль Unknown не считается выходом.</summary>
public sealed class PinDirectionRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext context)
    {
        // Этап 6–7: два выхода вместе, превышение тока GPIO, input-only как выход.
        // Без роли и сценария ни одно из этих нарушений статически не утверждается.
        return [];
    }
}
