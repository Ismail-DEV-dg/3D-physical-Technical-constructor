namespace CircuitEditor.Core.Diagnostics;

/// <summary>Подстановки обозначений, настроек и особенностей одной платы.</summary>
public sealed record GuideBoard(string Title, IReadOnlyDictionary<string, string> Values);
