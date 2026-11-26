using System.Text.RegularExpressions;

namespace CircuitEditor.Core.Diagnostics;

/// <summary>Общий каталог пособий и проверенных вариантов для плат.</summary>
public sealed class GuideLibrary
{
    private static readonly Regex Placeholder = new(@"\{\{([A-Z0-9_]+)\}\}", RegexOptions.CultureInvariant);

    /// <summary>Профили Nano, Uno и ESP32.</summary>
    public IReadOnlyDictionary<string, GuideBoard> Boards { get; init; } = new Dictionary<string, GuideBoard>();
    /// <summary>Главы, доступные для каждой платы.</summary>
    public IReadOnlyList<GuideChapter> Components { get; init; } = [];
    /// <summary>Главы по интерфейсу приложения.</summary>
    public IReadOnlyList<GuideChapter> Application { get; init; } = [];
    /// <summary>Учебные скетчи, которые можно запустить в редакторе.</summary>
    public IReadOnlyList<GuideChapter> Lessons { get; init; } = [];

    /// <summary>Собирает вариант пособия с номерами выводов выбранной платы.</summary>
    public IReadOnlyList<GuideChapter> Chapters(string section)
    {
        if (section == "application") return Application;
        if (section == "lessons") return Lessons;
        if (!Boards.TryGetValue(section, out var board)) return [];
        string Expand(string input) => Placeholder.Replace(input, match => board.Values[match.Groups[1].Value]);
        return Components.Select(chapter =>
        {
            var note = chapter.BoardNotes?.GetValueOrDefault(section);
            var instructions = note is null ? chapter.Instructions : $"{chapter.Instructions}\n\nДля {board.Title}\n{note}";
            return chapter with
            {
                Instructions = Expand(instructions),
                Code = chapter.Code is null ? null : Expand(chapter.Code),
                BoardNotes = null
            };
        }).ToArray();
    }
}
