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
    public static string Format(ChipTruthTable table)
    {
        StringBuilder text = new();
        foreach (var section in table.Sections)
        {
            text.AppendJoin(Delimiter, section.Inputs.Select(pin => InputPrefix + ChipPackage.PinId(pin))
                .Concat(section.Outputs.Select(pin => OutputPrefix + ChipPackage.PinId(pin)))).AppendLine();
            foreach (var pattern in ChipTruthLogic.Patterns(section.Inputs.Count))
            {
                var result = section.Rows.First(row => row[0].Zip(pattern)
                    .All(pair => pair.First == 'X' || pair.First == pair.Second))[1];
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

    private static string[] Cells(string line) => line.Split(Delimiter, StringSplitOptions.TrimEntries);
    private static int PinNumber(string column, string prefix) =>
        int.TryParse(column.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number > 0 ? number : throw new FormatException("Некорректный номер вывода в заголовке CSV.");
}
