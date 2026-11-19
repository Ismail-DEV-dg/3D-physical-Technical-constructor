using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class ChipTruthTableTests
{
    internal static IReadOnlyDictionary<string, LogicFamilyProfile> Families => new JsonLogicFamilyRepository().Load().Value!;

    internal static ChipDefinition Nand() => new(Guid.NewGuid(), 1, "NAND", "", "DIP-8", "ttl",
        [new(1,"A", ChipPinType.Input), new(2,"B", ChipPinType.Input),
         new(3,"Y", ChipPinType.Output), new(4,"GND", ChipPinType.Gnd),
         new(5,"NC5", ChipPinType.Input, IsUnused: true), new(6,"NC6", ChipPinType.Input, IsUnused: true),
         new(7,"NC7", ChipPinType.Input, IsUnused: true), new(8,"VCC", ChipPinType.Vcc)],
        new ChipTruthTable([new([1, 2], [3], [["0X","1"], ["10","1"], ["11","0"]])]));

    [Theory]
    [InlineData(ChipLevel.Low, ChipLevel.Low, ChipLevel.High)]
    [InlineData(ChipLevel.Low, ChipLevel.High, ChipLevel.High)]
    [InlineData(ChipLevel.High, ChipLevel.Low, ChipLevel.High)]
    [InlineData(ChipLevel.High, ChipLevel.High, ChipLevel.Low)]
    [InlineData(ChipLevel.Low, ChipLevel.Z, ChipLevel.High)]
    [InlineData(ChipLevel.High, ChipLevel.X, ChipLevel.X)]
    public void NandEvaluatesBinaryAndAmbiguousInputs(ChipLevel a, ChipLevel b, ChipLevel expected)
    {
        var chip = Nand();
        Assert.Empty(ChipValidator.Validate(chip, Families));
        Assert.Equal(expected, ChipTruthLogic.Evaluate((ChipTruthTable)chip.Behavior,
            new Dictionary<int, ChipLevel> { [1] = a, [2] = b })[3]);
    }

    [Fact]
    public void ValidatesIncompleteConflictingAndUnorderedRows()
    {
        var chip = Nand();
        var unordered = chip with { Behavior = new ChipTruthTable([new([2, 1], [3], [["XX","1"]])]) };
        Assert.Contains(ChipValidator.Validate(unordered, Families), error => error.Contains("возрастанию"));
        var incomplete = chip with { Behavior = new ChipTruthTable([new([1, 2], [3], [["00","X"]])]) };
        Assert.Contains(ChipValidator.Validate(incomplete, Families), error => error.Contains("«11»"));
        var conflict = chip with { Behavior = new ChipTruthTable([new([1, 2], [3], [["XX","1"], ["11","0"]])]) };
        Assert.Contains(ChipValidator.Validate(conflict, Families), error => error.Contains("«11»"));
        var unknown = chip with { Behavior = new ChipTruthTable([new([1, 2], [3], [["XX","X"]])]) };
        Assert.Empty(ChipValidator.Validate(unknown, Families));
        Assert.Equal(ChipLevel.X, ChipTruthLogic.Evaluate((ChipTruthTable)unknown.Behavior,
            new Dictionary<int, ChipLevel>())[3]);
    }

    [Fact]
    public void DipInsertsAcrossTheChannelWithNumberedContacts()
    {
        var chip = Nand();
        var footprint = ChipPackage.Footprint(chip);
        var project = BreadboardTestData.Create();
        var component = project.Circuit.AddComponent(ChipPackage.Component(chip, Families[chip.Family]));
        var board = project.Breadboards[0];
        var anchor = board.Definition.Holes.Single(hole => hole.Id == "left/E/02");
        var result = new ComponentInsertionService().Insert(project, component.Id, footprint, board.Id,
            new ComponentPlacement(board.X + anchor.Position.X, board.Y + anchor.Position.Y, 0));
        Assert.True(result.Success, result.Error);
        Assert.Equal("left/E/02", result.Bindings.Single(binding => binding.ContactId == "1").HoleId);
        Assert.Equal("left/E/05", result.Bindings.Single(binding => binding.ContactId == "4").HoleId);
        Assert.Equal("right/F/05", result.Bindings.Single(binding => binding.ContactId == "5").HoleId);
        Assert.Equal("right/F/02", result.Bindings.Single(binding => binding.ContactId == "8").HoleId);
        Assert.Equal(chip.Pins.Count, result.Bindings.Count);
    }

    [Fact]
    public void ProfilesDistinguishCurrentDirectionAndScaleCmosThresholds()
    {
        var ttl = Families["ttl"];
        Assert.False(ttl.ExceedsCurrentLimit(.010));
        Assert.True(ttl.ExceedsCurrentLimit(.017));
        Assert.True(ttl.ExceedsCurrentLimit(-.010));
        Assert.True(Families["ls"].ExceedsCurrentLimit(.010));
        var hc = Families["hc"];
        Assert.Equal(ChipLevel.High, hc.Read(2.5, 3.3));
        Assert.Equal(ChipLevel.X, hc.Read(2.5, 5));
        Assert.Equal(ChipLevel.Z, hc.Read(null, 5));
    }
}
