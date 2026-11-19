namespace CircuitEditor.Core.Chips;

/// <summary>Проверка, развёртывание и вычисление комбинационных таблиц.</summary>
public static class ChipTruthLogic
{
    /// <summary>Генерирует входные слова в двоичном порядке, от нулевой комбинации.</summary>
    public static IEnumerable<string> Patterns(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ChipTruthSection.MaximumInputs);
        if (count == 0) return [string.Empty];
        return Enumerable.Range(0, 1 << count).Select(value => Convert.ToString(value, 2).PadLeft(count, '0'));
    }

    internal static void Validate(ChipTruthTable table, IReadOnlySet<int> inputs,
        IReadOnlySet<int> outputs, IEnumerable<int> enablePins, List<string> errors)
    {
        var coveredInputs = enablePins.ToHashSet();
        HashSet<int> coveredOutputs = [];
        if (table.Sections is null || table.Sections.Count == 0)
        {
            errors.Add("Добавьте секцию таблицы истинности.");
            return;
        }

        for (var index = 0; index < table.Sections.Count; index++)
        {
            var section = table.Sections[index];
            var context = $"Секция {index + 1}";
            if (section is null || section.Inputs is null || section.Outputs is null || section.Rows is null
                || section.Inputs.Count > ChipTruthSection.MaximumInputs
                || section.Outputs.Count is < 1 or > ChipTruthSection.MaximumOutputs
                || !section.Inputs.SequenceEqual(section.Inputs.Distinct().Order())
                || section.Inputs.Any(pin => !inputs.Contains(pin))
                || section.Outputs.Any(pin => !outputs.Contains(pin) || !coveredOutputs.Add(pin)))
            {
                errors.Add($"{context}: проверьте входы по возрастанию, размеры секции и уникальность выходов.");
                continue;
            }
            coveredInputs.UnionWith(section.Inputs);
            if (section.Rows.Any(row => row is not { Count: 2 }
                || !ValidWord(row[0], section.Inputs.Count) || !ValidWord(row[1], section.Outputs.Count)))
            {
                errors.Add($"{context}: исправьте длину строк и символы (0, 1, X).");
                continue;
            }

            foreach (var pattern in Patterns(section.Inputs.Count))
            {
                var matches = section.Rows.Where(row => Matches(row[0], pattern))
                    .Select(row => row[1]).Distinct(StringComparer.Ordinal);
                if (matches.Take(2).Count() != 1)
                    errors.Add($"{context}: для входа «{pattern}» отсутствует однозначный результат.");
            }
        }

        if (!inputs.SetEquals(coveredInputs)) errors.Add("Есть входы, не привязанные к поведению.");
        if (!outputs.SetEquals(coveredOutputs)) errors.Add("Определите поведение всех выходов.");
    }

    /// <summary>Считает выходы проверенной таблицы, возвращая X только при неопределённости данного выхода.</summary>
    public static IReadOnlyDictionary<int, ChipLevel> Evaluate(ChipTruthTable table,
        IReadOnlyDictionary<int, ChipLevel> inputs)
    {
        Dictionary<int, ChipLevel> result = [];
        foreach (var section in table.Sections)
        {
            var current = string.Concat(section.Inputs.Select(pin => Symbol(inputs.GetValueOrDefault(pin, ChipLevel.Z))));
            var candidates = Patterns(section.Inputs.Count)
                .Where(pattern => Matches(current, pattern))
                .SelectMany(pattern => section.Rows.Where(row => Matches(row[0], pattern)).Select(row => row[1]))
                .ToArray();
            for (var i = 0; i < section.Outputs.Count; i++)
            {
                var levels = candidates.Select(output => output[i]).Distinct().Take(2).ToArray();
                result[section.Outputs[i]] = levels.Length == 1 ? Level(levels[0]) : ChipLevel.X;
            }
        }
        return result;
    }

    /// <summary>Обозначение дискретного уровня для интерфейса и экспорта.</summary>
    public static char Symbol(ChipLevel level) => level switch { ChipLevel.Low => '0', ChipLevel.High => '1', ChipLevel.Z => 'Z', _ => 'X' };

    internal static ChipLevel Level(char value) => value switch { '0' => ChipLevel.Low, '1' => ChipLevel.High, _ => ChipLevel.X };

    internal static string ResultFor(ChipTruthSection section, string pattern) =>
        section.Rows.First(row => Matches(row[0], pattern))[1];

    private static bool Matches(string candidate, string binary) =>
        candidate.Zip(binary).All(pair => pair.First is 'X' or 'Z' || pair.First == pair.Second);

    private static bool ValidWord(string? word, int length) =>
        word is not null && word.Length == length && word.All(letter => letter is '0' or '1' or 'X');
}
