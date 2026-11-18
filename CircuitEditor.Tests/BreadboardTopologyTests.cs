using System.IO;
using System.Text;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class BreadboardTopologyTests
{
    [Fact]
    public void HalfBoardHas400UniqueHolesAnd64DataDefinedGroups()
    {
        var result = new JsonBreadboardRepository().Load();
        Assert.True(result.Success, result.Error);
        var board = result.Value!;
        Assert.Equal(400, board.Holes.Count);
        Assert.Equal(400, board.Holes.Select(hole => hole.Id).Distinct().Count());
        Assert.Equal(64, board.Groups.Count);
        Assert.Equal(60, board.Groups.Count(group => group.HoleIds.Count == 5));
        Assert.Equal(4, board.Groups.Count(group => group.HoleIds.Count == 25));
        Assert.All(board.Holes, hole => Assert.Contains(board.Groups, group =>
            group.Id == hole.GroupId && group.HoleIds.Contains(hole.Id)));
    }

    [Theory]
    [InlineData("left/A/01", "left/E/01", true)]
    [InlineData("left/A/01", "right/F/01", false)]
    [InlineData("left/A/01", "left/A/02", false)]
    [InlineData("right/F/30", "right/J/30", true)]
    [InlineData("left-positive/01", "left-positive/25", true)]
    [InlineData("left-positive/01", "left-negative/01", false)]
    [InlineData("left-positive/01", "right-positive/01", false)]
    [InlineData("left-positive/01", "left/A/01", false)]
    [InlineData("missing", "missing", false)]
    public void ConnectivityComesFromContactGroups(string first, string second, bool connected)
    {
        var board = new JsonBreadboardRepository().Load().Value!;
        Assert.Equal(connected, board.AreConnected(first, second));
        var again = new JsonBreadboardRepository().Load().Value!;
        Assert.Equal(board.Holes.Select(hole => hole.Id), again.Holes.Select(hole => hole.Id));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{broken")]
    [InlineData("{\"schemaVersion\":1,\"id\":\"x\",\"units\":\"mm\",\"body\":{\"width\":50,\"height\":80},\"terminalBlocks\":[null],\"rails\":[]}")]
    public void InvalidTopologyProducesDiagnostic(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = JsonBreadboardRepository.Read(stream);
        Assert.False(result.Success); Assert.Null(result.Value); Assert.NotEmpty(result.Error!);
    }
}
