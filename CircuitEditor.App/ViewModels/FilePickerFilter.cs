using System.IO.Enumeration;

namespace CircuitEditor.App.ViewModels;

internal sealed record FilePickerFilter(string Label, IReadOnlyList<string> Patterns)
{
    public bool Includes(string fileName) => Patterns.Any(pattern =>
        pattern is "*" or "*.*" || FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true));

    public static IReadOnlyList<FilePickerFilter> Parse(string filter)
    {
        var parts = filter.Split('|');
        if (parts.Length < 2 || parts.Length % 2 != 0)
            return [new("Все файлы (*.*)", ["*.*"])];

        var filters = new List<FilePickerFilter>();
        for (var index = 0; index < parts.Length; index += 2)
        {
            var patterns = parts[index + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (patterns.Length > 0)
                filters.Add(new(parts[index], patterns));
        }
        return filters.Count > 0 ? filters : [new("Все файлы (*.*)", ["*.*"])];
    }
}
