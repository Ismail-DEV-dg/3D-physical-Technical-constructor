using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Validation;
/// <summary>Ищет обход токоограничения, а не просто наличие резистора где-либо в узле.</summary>
public sealed class LedResistorRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c)
    {
        foreach(var led in c.Project.Circuit.Components.Where(x=>x.Definition.Id is "led" or "rgb-led"))
        {
            var channels=led.Definition.Id=="led" ? new[]{("A","K",(string?)null)}
                : c.Catalog.Find(led.Definition.Id)!.ChannelProfiles!.Keys.Select(channel=>
                    led.Inputs.Values[Inputs.ComponentInput.RgbCommon]==1 ? ("COM",channel,(string?)channel) : (channel,"COM",(string?)channel)).ToArray();
            foreach(var (anode,cathode,channel) in channels)
            {
            var a=c.Nets.Find(led.Id,anode);var k=c.Nets.Find(led.Id,cathode);if(a is null || k is null) continue;
            var edges=new List<(string A,string B)>();
            foreach(var d in c.Project.Circuit.Components.Where(x=>x.Id!=led.Id && x.Definition.Id is "led" or "diode"))
                if(c.Nets.Find(d.Id,"A") is { } from && c.Nets.Find(d.Id,"K") is { } to) edges.Add((from.Id,to.Id));
            bool Reach(string start,string target)
            {
                var seen=new HashSet<string>();var queue=new Queue<string>();queue.Enqueue(start);
                while(queue.TryDequeue(out var node)) { if(node==target)return true;if(!seen.Add(node))continue;foreach(var e in edges.Where(e=>e.A==node))queue.Enqueue(e.B); }
                return false;
            }
            var vf=Electronics.ElectricalParameters.Led(led,c.Catalog,channel).Vf;
            bool Gpio(PinReference p)=>c.Pin(p).Type is PinType.DigitalIO or PinType.PWM;
            var starts=c.Sources.Where(s=>s.Voltage>vf).Select(s=>c.Nets.Find(s.ComponentId,s.Positive.PinId)!.Id)
                .Concat(c.Nets.Nets.Where(n=>n.Pins.Any(Gpio)).Select(n=>n.Id)).Distinct();
            var ends=c.Nets.Nets.Where(n=>n.Pins.Any(p=>c.Pin(p).Type==PinType.Ground||Gpio(p))).Select(n=>n.Id).ToArray();
            var unsafePath=starts.Any(start=>ends.Any(end=>start!=end && Reach(start,a.Id)&&Reach(k.Id,end)));
            if(unsafePath)
                yield return ErcContext.Issue("LED_NO_RESISTOR",$"{led.Reference}: возможен путь через LED без последовательного резистора (типичное Vf {vf:G} В).",
                    [new(led.Id,anode),new(led.Id,cathode)],[a.Id,k.Id],ErcSeverity.Warning);
            }
        }
    }
}
