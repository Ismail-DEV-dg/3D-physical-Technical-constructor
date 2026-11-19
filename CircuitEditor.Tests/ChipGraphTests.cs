using CircuitEditor.Core.Chips;

namespace CircuitEditor.Tests;

public sealed class ChipGraphTests
{
    private static ChipSignal Pin(int id) => new(Pin: id);
    private static ChipSignal Node(int id) => new(Gate: id);
    private static ChipSignal Zero => new(Constant: ChipLevel.Low);

    [Theory]
    [InlineData(ChipGateKind.And, "0001")]
    [InlineData(ChipGateKind.Or, "0111")]
    [InlineData(ChipGateKind.Nand, "1110")]
    [InlineData(ChipGateKind.Nor, "1000")]
    [InlineData(ChipGateKind.Xor, "0110")]
    [InlineData(ChipGateKind.Xnor, "1001")]
    public void BasicGatesMatchAllBinaryCombinations(ChipGateKind kind, string expected)
    {
        var chip = ChipTruthTableTests.Nand() with { Behavior = new ChipGraph([new(10, kind, [Pin(1), Pin(2)])], [new(3, Node(10))]) };
        Assert.Empty(ChipValidator.Validate(chip, ChipTruthTableTests.Families));
        var run = new ChipEvaluator().Compile(chip);
        var actual = ChipTruthLogic.Patterns(2).Select(word =>
            ChipTruthLogic.Symbol(run(Inputs(word[0] == '1', word[1] == '1'), ChipState.Empty).Outputs[3]));
        Assert.Equal(expected, string.Concat(actual));
    }

    [Fact]
    public void NotAndOrderedWiringPropagateUnknown()
    {
        var chip = ChipTruthTableTests.Nand() with
        {
            Behavior = new ChipGraph([new(2, ChipGateKind.And, [Node(1), Pin(2)]), new(1, ChipGateKind.Not, [Pin(1)])],
                [new(3, Node(2))])
        };
        Assert.Empty(ChipValidator.Validate(chip, ChipTruthTableTests.Families));
        var run = new ChipEvaluator().Compile(chip);
        Assert.Equal(ChipLevel.High, run(Inputs(false, true), ChipState.Empty).Outputs[3]);
        Assert.Equal(ChipLevel.X, run(new Dictionary<int, ChipLevel> { [2] = ChipLevel.High }, ChipState.Empty).Outputs[3]);
    }

    [Fact]
    public void DFlipFlopSamplesOnlyRisingEdgesAndSupportsMemoryFeedback()
    {
        var chip = ChipTruthTableTests.Nand() with
        {
            Behavior = new ChipGraph([new(10, ChipGateKind.D, [Node(20), Pin(2)], Pin(1)),
                new(20, ChipGateKind.Not, [Node(10)])], [new(3, Node(10))])
        };
        Assert.Empty(ChipValidator.Validate(chip, ChipTruthTableTests.Families));
        Assert.Equal("0011100", Run(chip, [(true,false),(false,false),(false,true),(false,true),
            (false,false),(false,true),(false,false)]));
    }

    [Fact]
    public void JkTogglesOnEdgeAndRsRetainsItsLevel()
    {
        var nand = ChipTruthTableTests.Nand();
        var jk = nand with
        {
            Behavior = new ChipGraph([new(1, ChipGateKind.Jk, [Pin(1), Pin(1), Pin(2)], new(Constant: ChipLevel.Low),
                Node(2)), new(2, ChipGateKind.Not, [Pin(1)])], [new(3, Node(1))])
        };
        var rs = nand with { Behavior = new ChipGraph([new(1, ChipGateKind.Rs, [Pin(1), Pin(2)])], [new(3, Node(1))]) };
        Assert.Empty(ChipValidator.Validate(jk, ChipTruthTableTests.Families));
        Assert.Empty(ChipValidator.Validate(rs, ChipTruthTableTests.Families));
        Assert.Equal("11001", Run(jk, [(false,false),(true,false),(true,true),(true,false),(true,true)]));
        Assert.Equal("1100X", Run(rs, [(true,false),(false,false),(false,true),(false,false),(true,true)]));
    }

    [Fact]
    public void RejectsCombinationalLoopsDepthConflictsAndUnconnectedPorts()
    {
        var chip = ChipTruthTableTests.Nand();
        var graph = new ChipGraph([new(1, ChipGateKind.Not, [Node(2)]), new(2, ChipGateKind.And, [Node(1), Pin(1), Pin(2)])],
            [new(3, Node(1))]);
        Assert.Contains(ChipValidator.Validate(chip with { Behavior = graph }, ChipTruthTableTests.Families),
            error => error.Contains("петля"));
        var deep = new[] { new ChipGate(1, ChipGateKind.Or, [Pin(1), Pin(2)]) }.Concat(Enumerable.Range(2, 50)
            .Select(index => new ChipGate(index, ChipGateKind.Not, [Node(index - 1)]))).ToArray();
        Assert.Contains(ChipValidator.Validate(chip with { Behavior = new ChipGraph(deep, [new(3, Node(51))]) },
            ChipTruthTableTests.Families), error => error.Contains("Глубина"));
        var invalid = new ChipGraph([new(1, ChipGateKind.And, [Zero])], [new(3, Node(1)), new(3, Pin(2))]);
        var errors = ChipValidator.Validate(chip with { Behavior = invalid }, ChipTruthTableTests.Families);
        Assert.Contains(errors, error => error.Contains("порты") || error.Contains("портов"));
        Assert.Contains(errors, error => error.Contains("драйвер"));
        Assert.Contains(errors, error => error.Contains("входы"));
    }

    [Fact]
    public void OpenCollectorReleasesHighAndBidirectionalDriverRespectsEnablePin()
    {
        var nand = ChipTruthTableTests.Nand();
        ChipDefinition Connect(ChipPinType type, int? enable = null) => nand with
        {
            Pins = nand.Pins.Select(pin => pin.Number == 3
                ? pin with { Type = type, EnablePin = enable, EnableLow = false } : pin).ToArray()
        };
        var evaluator = new ChipEvaluator();
        var openCollector = evaluator.Compile(Connect(ChipPinType.OpenCollector));
        Assert.Equal(ChipLevel.Z, openCollector(Inputs(false, false), ChipState.Empty).Outputs[3]);
        Assert.Equal(ChipLevel.Low, openCollector(Inputs(true, true), ChipState.Empty).Outputs[3]);
        var bidirectional = evaluator.Compile(Connect(ChipPinType.Bidirectional, 2));
        Assert.Equal(ChipLevel.Z, bidirectional(Inputs(false, false), ChipState.Empty).Outputs[3]);
        Assert.Equal(ChipLevel.High, bidirectional(Inputs(false, true), ChipState.Empty).Outputs[3]);
    }


    private static Dictionary<int, ChipLevel> Inputs(bool first, bool second) =>
        new() { [1] = first ? ChipLevel.High : ChipLevel.Low, [2] = second ? ChipLevel.High : ChipLevel.Low };

    private static string Run(ChipDefinition chip, IEnumerable<(bool A, bool B)> input)
    {
        var run = new ChipEvaluator().Compile(chip);
        var state = ChipState.Empty;
        return string.Concat(input.Select(sample =>
        {
            var result = run(Inputs(sample.A, sample.B), state);
            Assert.True(result.Settled);
            state = result.State;
            return ChipTruthLogic.Symbol(result.Outputs[3]);
        }));
    }
}
