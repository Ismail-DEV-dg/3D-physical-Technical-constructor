using System.Globalization;
using System.Text;

namespace CircuitEditor.Core.Chips;

/// <summary>Обмен секциями таблицы в CSV с отдельной колонкой для каждого вывода.</summary>
public static class ChipTruthCsv
{
    private const char Delimiter = ';';
    private const string InputPrefix = "IN:";
    private const string OutputPrefix = "OUT:";

    /// <summary>Разворачивает маски X во входах в полную таблицу с предсказуемым порядком колонок.</summary>
    public static string Format(ChipTruthTable table, IReadOnlyDictionary<int, string>? pinNames = null)
    {
        StringBuilder text = new();
        foreach (var section in table.Sections)
        {
            text.AppendJoin(Delimiter, section.Inputs.Select(pin => Column(InputPrefix, pin, pinNames))
                .Concat(section.Outputs.Select(pin => Column(OutputPrefix, pin, pinNames)))).AppendLine();
            foreach (var pattern in ChipTruthLogic.Patterns(section.Inputs.Count))
            {
                var result = ChipTruthLogic.ResultFor(section, pattern);
                text.AppendJoin(Delimiter, pattern.Concat(result)).AppendLine();
            }
            text.AppendLine();
        }
        return text.ToString();
    }

    /// <summary>Читает секции, разделённые пустой строкой. Электрическая привязка проверяется валидатором.</summary>
    public static ChipTruthTable Parse(string text)
    {
        List<ChipTruthSection> sections = [];
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } header)
        {
            if (string.IsNullOrWhiteSpace(header)) continue;
            var columns = Cells(header.TrimStart('\uFEFF'));
            var inputs = columns.TakeWhile(cell => cell.StartsWith(InputPrefix, StringComparison.Ordinal)).ToArray();
            var outputs = columns.Skip(inputs.Length).ToArray();
            if (inputs.Length > ChipTruthSection.MaximumInputs || outputs.Length is < 1 or > ChipTruthSection.MaximumOutputs
                || outputs.Any(cell => !cell.StartsWith(OutputPrefix, StringComparison.Ordinal)))
                throw new FormatException("Заголовок CSV должен содержать колонки IN:номер и OUT:номер.");
            List<IReadOnlyList<string>> rows = [];
            while (reader.ReadLine() is { } line && !string.IsNullOrWhiteSpace(line))
            {
                var cells = Cells(line);
                if (cells.Length != columns.Length || cells.Any(cell => cell is not ("0" or "1" or "X")))
                    throw new FormatException("В строках CSV допустимы только 0, 1 и X по числу колонок.");
                rows.Add([string.Concat(cells.Take(inputs.Length)), string.Concat(cells.Skip(inputs.Length))]);
                if (rows.Count > 1 << ChipTruthSection.MaximumInputs)
                    throw new FormatException("Слишком много строк в секции CSV.");
            }
            sections.Add(new(inputs.Select(cell => PinNumber(cell, InputPrefix)).ToArray(),
                outputs.Select(cell => PinNumber(cell, OutputPrefix)).ToArray(), rows));
        }
        if (sections.Count == 0) throw new FormatException("В CSV нет секций.");
        return new(sections);
    }

    private static string Column(string prefix, int pin, IReadOnlyDictionary<int, string>? names)
    {
        var value = prefix + ChipPackage.PinId(pin);
        if (names?.TryGetValue(pin, out var name) == true) value += ":" + name;
        return value.Contains(Delimiter) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"" : value;
    }

    private static string[] Cells(string line)
    {
        List<string> fields = [];
        StringBuilder cell = new();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (character == Delimiter && !quoted)
            {
                fields.Add(cell.ToString().Trim());
                cell.Clear();
            }
            else cell.Append(character);
        }
        if (quoted) throw new FormatException("Кавычки в CSV не закрыты.");
        fields.Add(cell.ToString().Trim());
        return fields.ToArray();
    }

    private static int PinNumber(string column, string prefix)
    {
        var token = column.AsSpan(prefix.Length);
        var separator = token.IndexOf(':');
        if (separator >= 0) token = token[..separator];
        return int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number : throw new FormatException("Некорректный номер вывода в заголовке CSV.");
    }
}
