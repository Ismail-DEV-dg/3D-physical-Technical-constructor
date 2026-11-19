using CircuitEditor.Core.Chips;

namespace CircuitEditor.Tests;

public sealed class ChipDocumentationTests
{
    [Fact]
    public void DocumentsAssignNamesToSortedColumnsAndEscapeSeparators()
    {
        var original = ChipTruthTableTests.Nand();
        var chip = original with { Pins = original.Pins.Select(pin => pin.Number == 1
            ? pin with { Name = "A; \"opt\" |", Description = "Input | <lab>" } : pin).Reverse().ToArray() };
        var table = Assert.IsType<ChipTruthTable>(chip.Behavior);
        var names = chip.Pins.ToDictionary(pin => pin.Number, pin => pin.Name);
        var csv = ChipTruthCsv.Format(table, names);
        Assert.StartsWith("\"IN:1:A; \"\"opt\"\" |\";IN:2:B;OUT:3:Y", csv);
        Assert.Equal(csv, ChipTruthCsv.Format(ChipTruthCsv.Parse(csv), names));
        var markdown = ChipMarkdown.Pinout(chip);
        Assert.Contains("Input \\| &lt;lab&gt;", markdown);
        Assert.True(markdown.IndexOf("| 1 |", StringComparison.Ordinal) < markdown.IndexOf("| 2 |", StringComparison.Ordinal));
        var truth = ChipMarkdown.Truth(chip, table);
        Assert.Contains("Вход 1 · A; \"opt\" \\| | Вход 2 · B | Выход 3 · Y", truth);
        Assert.Contains("| 1 | 1 | 0 |", truth);
    }

    [Fact]
    public void DocumentationIncludesAllFourNandSectionsWithAscendingPinOrder()
    {
        var chip = ChipTemplateTests.Get("7400");
        var table = Assert.IsType<ChipTruthTable>(chip.Behavior);
        var truth = ChipMarkdown.Truth(chip, table);
        Assert.Contains("| Вход 9 · 3A | Вход 10 · 3B | Выход 8 · 3Y |", truth);
        Assert.Contains("| Вход 12 · 4A | Вход 13 · 4B | Выход 11 · 4Y |", truth);
        Assert.Equal(4, truth.Split("## Секция ", StringSplitOptions.None).Length - 1);
    }
}
