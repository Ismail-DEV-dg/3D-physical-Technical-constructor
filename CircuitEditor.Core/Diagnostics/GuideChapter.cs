namespace CircuitEditor.Core.Diagnostics;

/// <summary>Статья справки с монтажом, проверкой и примером скетча.</summary>
public sealed record GuideChapter(string Id, string Title, string Instructions, string? Code = null,
    IReadOnlyDictionary<string, string>? BoardNotes = null, string? CodePurpose = null);
