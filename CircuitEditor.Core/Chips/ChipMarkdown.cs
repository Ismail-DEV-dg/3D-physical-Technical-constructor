using System.Text;

namespace CircuitEditor.Core.Chips;

/// <summary>Документация микросхемы в Markdown для пояснительной записки.</summary>
public static class ChipMarkdown
{
    /// <summary>Таблица выводов в порядке нумерации корпуса.</summary>
    public static string Pinout(ChipDefinition definition)
    {
        var text = Heading(definition, "Распиновка");
        Row(text, ["Номер", "Имя", "Тип", "Описание"]);
        Row(text, ["---", "---", "---", "---"]);
        foreach (var pin in definition.Pins.OrderBy(pin => pin.Number))
            Row(text, [ChipPackage.PinId(pin.Number), Escape(pin.Name), pin.IsUnused ? "NC" : TypeName(pin.Type), Escape(pin.Description)]);
        return text.ToString();
    }

    /// <summary>Развёрнутая таблица логических уровней по секциям, с номерами и именами выводов.</summary>
    public static string Truth(ChipDefinition definition, ChipTruthTable table)
    {
        var text = Heading(definition, "Таблица истинности");
        var pins = definition.Pins.ToDictionary(pin => pin.Number, pin => pin.Name);
        foreach (var (section, index) in table.Sections.Select((section, index) => (section, index)))
        {
            text.Append("## Секция ").AppendLine((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
            var inputs = section.Inputs.Select(pin => $"Вход {pin} · {Escape(pins[pin])}");
            var outputs = section.Outputs.Select(pin => $"Выход {pin} · {Escape(pins[pin])}");
            Row(text, inputs.Concat(outputs));
            Row(text, Enumerable.Repeat("---", section.Inputs.Count + section.Outputs.Count));
            foreach (var input in ChipTruthLogic.Patterns(section.Inputs.Count))
                Row(text, input.Concat(ChipTruthLogic.ResultFor(section, input)).Select(bit => bit.ToString()));
            text.AppendLine();
        }
        return text.ToString();
    }

    /// <summary>Подпись типа вывода в документации.</summary>
    public static string TypeName(ChipPinType kind) => kind switch
    {
        ChipPinType.Vcc => "VCC", ChipPinType.Gnd => "GND", ChipPinType.OpenCollector => "Open Collector",
        _ => kind.ToString()
    };

    private static StringBuilder Heading(ChipDefinition chip, string title) =>
        new StringBuilder().Append("# ").Append(title).Append(" · ").AppendLine(Escape(chip.Name))
            .AppendLine().Append(chip.Package).Append(" · ").AppendLine(Escape(chip.Family)).AppendLine();

    private static void Row(StringBuilder text, IEnumerable<string> columns) =>
        text.Append("| ").AppendJoin(" | ", columns).AppendLine(" |");

    private static string Escape(string? text) => (text ?? "").Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}
