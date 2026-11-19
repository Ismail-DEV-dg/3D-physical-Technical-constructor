namespace CircuitEditor.Core.Code;
/// <summary>Версионированный набор профилей и шаблонов генерации.</summary>
public sealed record CodeCatalog(int SchemaVersion, IReadOnlyList<CodeBoardProfile> Boards, IReadOnlyList<CodeTemplate> Templates);
