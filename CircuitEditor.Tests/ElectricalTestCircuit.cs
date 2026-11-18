using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Validation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
internal sealed class ElectricalTestCircuit : IDisposable
{
    public Stage4TestEnvironment Environment { get; }=new();
    public ProjectViewModel Project=>Environment.Project;
    public ComponentViewModel Add(string id)=>Project.Add(Project.Library.Single(d=>d.Id==id))!;
    public void Wire(ComponentViewModel a,string pinA,ComponentViewModel b,string pinB)
    {
        var ca=a.Footprint.Contacts.First(c=>c.ElectricalPinId==pinA);var cb=b.Footprint.Contacts.First(c=>c.ElectricalPinId==pinB);
        Project.Model.Circuit.PutWire(new Wire(WireEndpoint.Contact(a.Model.Id,ca.Id),WireEndpoint.Contact(b.Model.Id,cb.Id)));
    }
    public Core.Netlist.Netlist Nets()=>new NetlistBuilder(new JsonElectricalLibraryRepository()).Build(Project.Model);
    public IReadOnlyList<ErcIssue> Check(IErcRule rule)=>rule.Check(new ErcContext(Project.Model,Nets(),new JsonElectricalLibraryRepository().Load().Value!)).ToArray();
    public void Dispose()=>Environment.Dispose();
}
