using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Validation;
/// <summary>Диагностика с адресами проблемных выводов, экземпляров и узлов.</summary>
public sealed record ErcIssue(ErcSeverity Severity, string Code, string Message,
    IReadOnlyList<Guid> Components, IReadOnlyList<PinReference> Pins, IReadOnlyList<string> Nets);
