namespace CircuitEditor.Core.Code;
/// <summary>Атомарный результат генерации с предупреждениями и подписью схемы.</summary>
public sealed record CodeGenerationResult(bool Success, string Text, string? CircuitSignature, IReadOnlyList<CodeProblem> Problems);
