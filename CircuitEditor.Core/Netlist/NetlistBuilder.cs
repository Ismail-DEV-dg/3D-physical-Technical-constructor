using System.Security.Cryptography;
using System.Text;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.Core.Netlist;
/// <summary>Union-find для выводов, корпусов, breadboard и перемычек.</summary>
public sealed class NetlistBuilder(IElectricalLibraryRepository repository) : INetlistBuilder
{
    /// <inheritdoc/>
    public Netlist Build(CircuitProject project)
    {
        var catalog=repository.Load(); var uf=new UnionFind(); var terminals=new Dictionary<string,NetTerminal>();
        var diagnostics=new List<string>(); if(catalog.Error is not null) diagnostics.Add(catalog.Error);
        string Add(string kind,Guid owner,string id)
        { var t=new NetTerminal(kind,owner,id); terminals.TryAdd(t.Key,t); uf.Find(t.Key); return t.Key; }
        string Endpoint(WireEndpoint e)=>Add(e.Kind==WireEndpointKind.ComponentContact?"c":"h",e.OwnerId,e.TerminalId);
        foreach(var board in project.Breadboards)
            foreach(var group in board.Definition.Holes.GroupBy(h=>h.GroupId))
            {
                string? first=null;
                foreach(var hole in group) { var key=Add("h",board.Id,hole.Id); if(first is null) first=key; else uf.Join(first,key); }
            }
        foreach(var component in project.Circuit.Components)
        {
            foreach(var pin in component.Definition.Pins.Where(p=>p.IsExposed && !p.IsNoConnect)) Add("p",component.Id,pin.Id);
            var footprint=project.Footprints.FirstOrDefault(f=>f.ComponentDefinitionId==component.Definition.Id);
            if(footprint is not null)
                foreach(var contact in footprint.Contacts)
                {
                    var physical=Add("c",component.Id,contact.Id);
                    if(component.Definition.Pins.Any(p=>p.Id==contact.ElectricalPinId && !p.IsNoConnect)) uf.Join(physical,Add("p",component.Id,contact.ElectricalPinId));
                }
            foreach(var link in catalog.Value?.Find(component.Definition.Id)?.Links ?? [])
            {
                if(link.Input is not null)
                {
                    var value=Enum.TryParse<ComponentInput>(link.Input,out var input) ? component.Inputs.Values.GetValueOrDefault(input) : 0;
                    if(value!=link.Value) continue;
                }
                var keys=link.Pins.Select(p=>Add("p",component.Id,p)).ToArray();
                foreach(var key in keys.Skip(1)) uf.Join(keys[0],key);
            }
        }
        foreach(var binding in project.Bindings) uf.Join(Add("c",binding.ComponentId,binding.ContactId),Add("h",binding.BoardId,binding.HoleId));
        foreach(var wire in project.Circuit.Wires)
        {
            if(WireGeometry.Resolve(project,wire.Start) is null || WireGeometry.Resolve(project,wire.End) is null)
            { diagnostics.Add("Не найден конец перемычки "+wire.Id); continue; }
            uf.Join(Endpoint(wire.Start),Endpoint(wire.End));
        }
        var index=0; var names=new Dictionary<string,int>();
        var nets=terminals.Values.GroupBy(t=>uf.Find(t.Key)).Select(g=>g.OrderBy(t=>t.Key,StringComparer.Ordinal).ToArray())
            .OrderBy(g=>g[0].Key,StringComparer.Ordinal).Select(group=>
            {
                var pins=group.Where(t=>t.Kind=="p").Select(t=>new PinReference(t.OwnerId,t.TerminalId)).ToArray();
                var name=pins.Any(p=>project.Circuit.Components.Single(c=>c.Id==p.ComponentId).Definition.Pins.Any(x=>x.Id==p.PinId && x.Type==PinType.Ground)) ? "GND"
                    : pins.Any(p=>p.PinId=="5V") ? "5V" : pins.Any(p=>p.PinId=="3V3") ? "3V3" : "Net"+(++index);
                var count=names.GetValueOrDefault(name)+1; names[name]=count; if(count>1) name+="_"+count;
                var id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|",group.Select(t=>t.Key)))));
                return new Net(id,name,Array.AsReadOnly(group),Array.AsReadOnly(pins));
            }).ToArray();
        return new(Array.AsReadOnly(nets),diagnostics.AsReadOnly());
    }
}
