using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class NetlistTests
{
    private static NetlistBuilder Builder()=>new(new JsonElectricalLibraryRepository());
    [Fact]
    public void BreadboardGroupsChannelRailsAndWireAreRespected()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project.Model; var b=e.Mockup.Board!;
        var n=Builder().Build(p);
        Assert.Equal(b.Definition.Groups.Count,n.Nets.Count);
        Net Hole(string id)=>n.Nets.Single(x=>x.Terminals.Any(t=>t.Kind=="h" && t.TerminalId==id));
        Assert.Equal(Hole("left/A/01").Id,Hole("left/E/01").Id);
        Assert.NotEqual(Hole("left/A/01").Id,Hole("right/F/01").Id);
        Assert.NotEqual(Hole("left/A/01").Id,Hole("left/A/02").Id);
        foreach(var g in b.Definition.Holes.GroupBy(h=>h.GroupId)) Assert.Single(g.Select(h=>Hole(h.Id).Id).Distinct());
        p.Circuit.PutWire(new Wire(WireEndpoint.Hole(b.Id,"left/A/01"),WireEndpoint.Hole(b.Id,"left/A/02")));
        n=Builder().Build(p); Assert.Equal(Hole("left/A/01").Id,Hole("left/A/02").Id);
    }
    [Fact]
    public void FourButtonContactsMapToTwoPinsAndGroundsStayLocalToBoard()
    {
        using var e=new Stage4TestEnvironment();
        var button=e.Project.Add(e.Project.Library.Single(d=>d.Id=="button"))!;
        var uno=e.Project.Add(e.Project.Library.Single(d=>d.Id=="arduino-uno"))!;
        var second=e.Project.Add(uno.Model.Definition)!; var n=Builder().Build(e.Project.Model);
        Assert.NotEqual(n.Find(button.Model.Id,"1"),n.Find(button.Model.Id,"2"));
        foreach(var group in button.Footprint.Contacts.GroupBy(c=>c.ElectricalPinId))
            Assert.All(group,c=>Assert.Contains(n.Find(button.Model.Id,group.Key)!.Terminals,t=>t.Kind=="c" && t.TerminalId==c.Id));
        Assert.Equal(n.Find(uno.Model.Id,"GND"),n.Find(uno.Model.Id,"GND2"));
        Assert.NotEqual(n.Find(uno.Model.Id,"GND"),n.Find(second.Model.Id,"GND"));
        Assert.NotEqual(n.Find(uno.Model.Id,"5V"),n.Find(uno.Model.Id,"3V3"));
    }
    [Fact]
    public void MovingInsertedPartChangesMembershipAndStableBuildKeepsIds()
    {
        using var e=new Stage4TestEnvironment();var c=e.Project.Add(e.Project.Library.Single(d=>d.Id=="resistor"))!;
        var b=e.Mockup.Board!;var h=b.Definition.Holes.Single(h=>h.Id=="left/A/01");
        Assert.True(e.Mockup.TryPlace(c,new(b.X+h.Position.X,b.Y+h.Position.Y,90)));
        var first=Builder().Build(e.Project.Model);Assert.Equal(first.Nets.Select(n=>n.Id),Builder().Build(e.Project.Model).Nets.Select(n=>n.Id));
        Assert.True(e.Mockup.TryPlace(c,new(c.MockupPlacement.X,c.MockupPlacement.Y+b.Definition.HolePitch,90)));
        Assert.NotEqual(first.Find(c.Model.Id,"1")!.Id,Builder().Build(e.Project.Model).Find(c.Model.Id,"1")!.Id);
    }
    [Fact]
    public void LedForwardVoltagesComeFromData()
    {
        var data=new JsonElectricalLibraryRepository().Load();Assert.True(data.Success,data.Error);
        Assert.Equal(new[]{2,2.1,2.2,3,3},data.Value!.LedProfiles.Select(p=>p.Vf));
    }
    [Fact]
    public void ButtonAndRelayStaticStatesSelectOnlyTheIntendedContacts()
    {
        using var e=new ElectricalTestCircuit();var button=e.Add("button");var relay=e.Add("relay");
        var before=e.Nets();Assert.NotEqual(before.Find(button.Model.Id,"1"),before.Find(button.Model.Id,"2"));
        Assert.Equal(before.Find(relay.Model.Id,"COM"),before.Find(relay.Model.Id,"NC"));
        button.Model.TrySetInput(Core.Inputs.ComponentInput.ButtonPressed,1);relay.Model.TrySetInput(Core.Inputs.ComponentInput.RelayEnergized,1);
        var after=e.Nets();Assert.Equal(after.Find(button.Model.Id,"1"),after.Find(button.Model.Id,"2"));
        Assert.Equal(after.Find(relay.Model.Id,"COM"),after.Find(relay.Model.Id,"NO"));
        Assert.NotEqual(after.Find(relay.Model.Id,"COM"),after.Find(relay.Model.Id,"NC"));
    }

}
