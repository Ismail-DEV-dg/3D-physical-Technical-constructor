using CircuitEditor.Core.Chips;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class ChipTemplateTests
{
    internal static ChipDefinition Get(string name)
    {
        var library = new JsonChipTemplateRepository(new JsonLogicFamilyRepository()).Load();
        Assert.True(library.Success, library.Error);
        Assert.Equal(12, library.Value!.Count);
        return library.Value.Single(chip => chip.Name == name);
    }

    [Theory]
    [InlineData("7400", "1110", 4)]
    [InlineData("7402", "1000", 4)]
    [InlineData("7404", "10", 6)]
    [InlineData("7408", "0001", 4)]
    [InlineData("7432", "0111", 4)]
    [InlineData("7486", "0110", 4)]
    public void LogicTemplatesMatchGoldenTables(string name, string expected, int sections)
    {
        var chip = Get(name);
        var table = Assert.IsType<ChipTruthTable>(chip.Behavior);
        Assert.Equal(sections, table.Sections.Count);
        Assert.Equal("ttl", chip.Family);
        foreach (var section in table.Sections)
        {
            Assert.Equal(section.Inputs.Order(), section.Inputs);
            Assert.All(ChipTruthLogic.Patterns(section.Inputs.Count).Select((word, index) => (word, index)), sample =>
            {
                Assert.Equal(expected[sample.index], ChipTruthLogic.Symbol(
                    new ChipEvaluator().Compile(chip)(Sample(section.Inputs, sample.word), ChipState.Empty).Outputs[section.Outputs[0]]));
            });
        }
    }

    [Theory]
    [InlineData("MUX-4")]
    [InlineData("DECODER-2")]
    [InlineData("FULL-ADDER")]
    public void CombinationalTemplatesMatchEveryInput(string name)
    {
        var chip = Get(name);
        var section = Assert.Single(Assert.IsType<ChipTruthTable>(chip.Behavior).Sections);
        var evaluate = new ChipEvaluator().Compile(chip);
        foreach (var bits in ChipTruthLogic.Patterns(section.Inputs.Count))
        {
            int Number(char c) => c - '0';
            var selection = Number(bits[0]) + 2 * Number(bits[1]);
            var sum = bits.Count(bit => bit == '1');
            var expected = name switch
            {
                "MUX-4" => bits[2 + selection].ToString(),
                "DECODER-2" => new string('0', 4).Remove(selection, 1).Insert(selection, "1"),
                _ => sum switch { 0 => "00", 1 => "10", 2 => "01", _ => "11" }
            };
            var evaluated = evaluate(Sample(section.Inputs, bits), ChipState.Empty);
            Assert.Equal(expected, string.Concat(section.Outputs.Select(pin => ChipTruthLogic.Symbol(evaluated.Outputs[pin]))));
        }
    }

    [Fact]
    public void FlipFlopTemplateHasIndependentEdgesAndAsynchronousControls()
    {
        var evaluate = new ChipEvaluator().Compile(Get("7474"));
        var pins = new[] { 1, 2, 3, 4, 10, 11, 12, 13 };
        var state = ChipState.Empty;
        string Run(string input)
        {
            var result = evaluate(Sample(pins, input), state);
            state = result.State;
            return Word(result, 5, 6, 9, 8);
        }
        Assert.Equal("0110", Run("01010001"));
        Assert.Equal("0110", Run("11011001"));
        Assert.Equal("1010", Run("11111001"));
        Assert.Equal("1001", Run("10111101"));
        Assert.Equal("XX01", Run("00101101"));
    }

    [Fact]
    public void CounterTemplateCountsAllSixteenStatesAndWraps()
    {
        var evaluate = new ChipEvaluator().Compile(Get("COUNTER-4"));
        var state = evaluate(Sample([1, 2], "01"), ChipState.Empty).State;
        var pins = new[] { 6, 5, 4, 3 };
        foreach (var count in Enumerable.Range(1, 16))
        {
            var low = evaluate(Sample([1, 2], "00"), state);
            var high = evaluate(Sample([1, 2], "10"), low.State);
            Assert.Equal(Convert.ToString(count % 16, 2).PadLeft(4, '0'), Word(high, pins));
            state = high.State;
        }
    }

    [Fact]
    public void Hc595SeparatesShiftAndLatchAndOeOnlyDisablesParallelOutputs()
    {
        var evaluate = new ChipEvaluator().Compile(Get("74HC595"));
        var state = ChipState.Empty;
        var inputs = new[] { 10, 11, 12, 13, 14 };
        var outputs = new[] { 15, 1, 2, 3, 4, 5, 6, 7, 9 };
        string Run(string bits)
        {
            var result = evaluate(Sample(inputs, bits), state);
            state = result.State;
            return Word(result, outputs);
        }
        Assert.Equal("XXXXXXXX0", Run("00000"));
        foreach (var bit in "10110010")
        {
            Assert.EndsWith("0", Run("1000" + bit));
            Run("1100" + bit);
        }
        Assert.Equal("XXXXXXXX1", Run("10000"));
        Assert.Equal("010011010", Run("11100"));
        Assert.Equal("ZZZZZZZZ0", Run("11110"));
        Assert.Equal("010011010", Run("10100"));
        Assert.Equal("010011010", Run("00100"));
        Run("10000");
        Assert.Equal("000000000", Run("10100"));
    }

    private static Dictionary<int, ChipLevel> Sample(IReadOnlyList<int> pins, string levels) =>
        pins.Select((pin, index) => (pin, value: levels[index] == '1' ? ChipLevel.High : ChipLevel.Low))
            .ToDictionary(item => item.pin, item => item.value);

    private static string Word(ChipEvaluation evaluation, params int[] pins) =>
        string.Concat(pins.Select(pin => ChipTruthLogic.Symbol(evaluation.Outputs[pin])));
}
