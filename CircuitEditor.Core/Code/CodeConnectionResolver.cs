using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Code;
/// <summary>Поиск сигнала через Netlist и ограниченные пассивные цепочки.</summary>
public static class CodeConnectionResolver
{
    /// <summary>Находит единственный доступный GPIO; через резистор проходит только по явному разрешению.</summary>
    public static BoardPinDefinition? Find(CodeCircuitContext context, Guid component, string pin, bool throughResistor = false)
    {
        var starts=pin.Split('|').Select(p=>context.Nets.Find(component,p)).Where(n=>n is not null).Select(n=>n!.Id).ToArray();
        var visited=starts.ToHashSet();var queue=new Queue<string>(starts);var found=new List<BoardPinDefinition>();
        while(queue.TryDequeue(out var id))
        {
            var net=context.Nets.Nets.Single(n=>n.Id==id);
            found.AddRange(net.Pins.Where(p=>p.ComponentId==context.Controller.Id)
                .Select(p=>context.Profile.Pins.FirstOrDefault(x=>x.Id==p.PinId&&x.IsExposed)).OfType<BoardPinDefinition>());
            if(!throughResistor)continue;
            foreach(var resistor in context.Project.Circuit.Components.Where(c=>c.Definition.Id=="resistor" && net.Pins.Any(p=>p.ComponentId==c.Id)))
                foreach(var next in resistor.Definition.Pins.Select(p=>context.Nets.Find(resistor.Id,p.Id)).OfType<Netlist.Net>())
                    if(visited.Add(next.Id))queue.Enqueue(next.Id);
        }
        var candidates=found.DistinctBy(p=>p.Number).ToArray();return candidates.Length==1 ? candidates[0] : null;
    }
    /// <summary>Определяет подключение второго вывода кнопки к земле контроллера.</summary>
    public static bool ButtonHasGround(CodeCircuitContext context, Component component) => component.Definition.Pins
        .Select(p=>context.Nets.Find(component.Id,p.Id)).OfType<Netlist.Net>().Any(n=>n.Pins.Any(p=>p.ComponentId==context.Controller.Id
            && context.Controller.Definition.Pins.Any(x=>x.Id==p.PinId&&x.Type==PinType.Ground)));
    /// <summary>Распознаёт ограниченный NPN-драйвер реле: катушка—коллектор, база через резистор к GPIO.</summary>
    public static BoardPinDefinition? FindRelayControl(CodeCircuitContext context, Component relay)
    {
        var coil=context.Nets.Find(relay.Id,"COIL-");
        var drivers=context.Project.Circuit.Components.Where(c=>c.Definition.Id=="npn"
            && context.Nets.Find(c.Id,"C")?.Id==coil?.Id).ToArray();
        if(coil is null||drivers.Length!=1)return null;
        return Find(context,drivers[0].Id,"B",true);
    }
}
