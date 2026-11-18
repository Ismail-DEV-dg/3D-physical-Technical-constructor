namespace CircuitEditor.Core.Validation;
/// <summary>Расширяемое статическое правило.</summary>
public interface IErcRule
{
    /// <summary>Проверяет снимок модели и связности.</summary>
    IEnumerable<ErcIssue> Check(ErcContext context);
}
