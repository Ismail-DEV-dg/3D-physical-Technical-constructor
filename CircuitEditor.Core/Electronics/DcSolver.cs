using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Core.Electronics;
/// <summary>Разделение на электрические острова и MNA с независимой точкой отсчёта в каждом.</summary>
public sealed class DcSolver(IElectricalLibraryRepository repository) : IDcSolver
{
    private const double ResistorPowerLimit=.25;
    /// <inheritdoc/>
    public DcResult Solve(CircuitProject project,Netlist.Netlist nets)
    {
        var voltages=nets.Nets.ToDictionary(n=>n.Id,_=>(double?)null);var results=new List<DcBranchResult>();var messages=nets.Diagnostics.ToList();
        var data=repository.Load();if(data.Value is null)return new(false,voltages,results,[data.Error??"Нет электрической библиотеки."],[]);
        var assembly=DcCircuitAssembler.Assemble(project,nets,data.Value);messages.AddRange(assembly.Diagnostics);
        var remaining=assembly.Branches.ToList();var success=nets.Diagnostics.Count==0 && !assembly.HasErrors;
        while(remaining.Count>0)
        {
            var nodes=new HashSet<string>{remaining[0].A};var count=0;
            while(count!=nodes.Count){count=nodes.Count;foreach(var b in remaining.Where(b=>nodes.Contains(b.A)||nodes.Contains(b.B))){nodes.Add(b.A);nodes.Add(b.B);}}
            var branches=remaining.Where(b=>nodes.Contains(b.A)).ToArray();remaining.RemoveAll(b=>nodes.Contains(b.A));
            var ground=nets.Nets.FirstOrDefault(n=>nodes.Contains(n.Id)&&n.Pins.Any(p=>project.Circuit.Components.Single(c=>c.Id==p.ComponentId).Definition.Pins.Any(pin=>pin.Id==p.PinId&&pin.Type==PinType.Ground)));
            if(ground is null){messages.Add("Плавающий остров без пути к земле: "+string.Join(", ",branches.Select(b=>b.Name)));success=false;continue;}
            // Identical parallel ideal sources fix voltage but cannot determine individual currents.
            var unique=new List<DcBranch>();var redundant=new List<DcBranch>();
            foreach(var b in branches)
                if(b.Kind=="V" && unique.Any(v=>v.Kind=="V"&&v.A==b.A&&v.B==b.B&&v.Value==b.Value))redundant.Add(b);else unique.Add(b);
            var solved=DcIslandSolver.Solve(unique,ground.Id);
            if(solved.Error is not null){messages.Add(solved.Error);success=false;continue;}
            foreach(var pair in solved.Voltages!)voltages[pair.Key]=pair.Value;
            foreach(var result in solved.Branches)
            {
                var ambiguous=unique.Any(b=>b.Name==result.Name&&b.Kind=="V"&&redundant.Any(r=>r.A==b.A&&r.B==b.B));
                results.Add(ambiguous ? result with {Current=null,Power=null}:result);
            }
            foreach(var b in redundant){results.Add(new(b.ComponentId,b.Name,b.A,b.B,b.Value,null,null));messages.Add(b.Name+": ток параллельных идеальных источников не определяется однозначно.");}
        }
        foreach(var result in results)
        {
            var branch=assembly.Branches.First(b=>b.Name==result.Name);var component=project.Circuit.Components.Single(c=>c.Id==result.ComponentId);
            if(branch.Kind=="LED" && result.Current>branch.MaximumCurrent)messages.Add(result.Name+": ток LED выше допустимого.");
            if(component.Definition.Id=="resistor" && result.Power>ResistorPowerLimit)messages.Add(result.Name+": мощность резистора выше 0.25 Вт.");
        }
        if(assembly.Branches.Count==0){messages.Add("Нет поддерживаемых DC-ветвей.");success=false;}
        return new(success,voltages,results,messages,assembly.Unsupported);
    }
}
