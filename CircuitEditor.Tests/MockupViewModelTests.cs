using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Tests;

public sealed class MockupViewModelTests
{
    [Fact]
    public void BothViewsShareSelectionButMovingSymbolKeepsPhysicalInsertion()
    {
        using var shared = TestProjectFactory.Create();
        using var mockup = TestProjectFactory.Mockup(shared);
        using var schematic = new CircuitEditor.App.ViewModels.SchematicViewModel(shared);
        var item = schematic.AddComponent(shared.Library.Single(item => item.Id == "resistor"), 100, 100);
        Assert.Same(item, mockup.Project.SelectedComponent);
        var board = mockup.Board!;
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/A/01");
        Assert.True(mockup.TryPlace(item, new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, 4)));
        var before = item.MockupPlacement.Copy();
        item.SchematicPlacement.MoveTo(800, 500);
        shared.Refresh();
        Assert.Equal(before.X, item.MockupPlacement.X);
        Assert.Equal(before.Y, item.MockupPlacement.Y);
        Assert.Equal(2, shared.Model.Bindings.Count);
        Assert.Same(item, schematic.SelectedComponent);
        Assert.Single(shared.Model.Circuit.Components);
    }

    [Fact]
    public void ChangingSpanUpdatesBothPhysicalLeadsAndOccupiedHolesOrRollsBack()
    {
        using var shared = TestProjectFactory.Create();
        using var mockup = TestProjectFactory.Mockup(shared);
        var item = shared.Add(shared.Library.Single(item => item.Id == "resistor"))!;
        var board = mockup.Board!;
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/A/01");
        Assert.True(mockup.TryPlace(item, new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, 4)));
        mockup.SelectedSpan = 8;
        Assert.Equal(8, item.MockupPlacement.SpanHoles);
        Assert.Contains(shared.Model.Bindings, binding => binding.HoleId == "left/A/09");
        var second = ContactGeometry.World(item.Footprint, item.Footprint.Contacts[1], item.MockupPlacement, board.Definition.HolePitch);
        Assert.Equal(board.Y + hole.Position.Y + 8 * board.Definition.HolePitch, second.Y, 8);
        var pose = item.MockupPlacement.Copy();
        mockup.SelectedSpan = 13;
        Assert.Equal(pose.SpanHoles, item.MockupPlacement.SpanHoles);
        Assert.False(mockup.TryPlace(item, new ComponentPlacement(pose.X, pose.Y, 90, 13)));
        Assert.Equal(pose.SpanHoles, item.MockupPlacement.SpanHoles);
    }

    [Theory]
    [InlineData("220 Ω", "#D32F2F", "#D32F2F", "#704020")]
    [InlineData("4.7 kΩ", "#F7D12E", "#843DB0", "#D32F2F")]
    [InlineData("10 kΩ", "#704020", "#171717", "#F27822")]
    public void ResistorBodyBandsFollowNominal(string value, string first, string second, string multiplier)
    {
        var bands = ResistorMarking.GetBands(value);
        Assert.Equal(4, bands.Count);
        Assert.Equal(first, bands[0]); Assert.Equal(second, bands[1]); Assert.Equal(multiplier, bands[2]);
    }
}
