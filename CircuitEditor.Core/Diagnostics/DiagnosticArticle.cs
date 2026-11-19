namespace CircuitEditor.Core.Diagnostics;
/// <summary>Причина и способ исправления известной ошибки приложения.</summary>
public sealed record DiagnosticArticle(string Code,string Title,string Cause,string Fix,IReadOnlyList<string> Keywords,
    string? ComponentGuideId = null, string? ApplicationGuideId = null);
