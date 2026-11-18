using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Netlist;
/// <summary>Неизменяемый результат построения графа.</summary>
public sealed class Netlist(IReadOnlyList<Net> nets, IReadOnlyList<string>? diagnostics = null)
{
    private readonly Dictionary<PinReference,Net> pins=nets.SelectMany(n=>n.Pins.Select(p=>(p,n))).ToDictionary(x=>x.p,x=>x.n);
    /// <summary>Все узлы, включая свободные контакты breadboard.</summary>
    public IReadOnlyList<Net> Nets { get; } = nets;
    /// <summary>Ошибки исходных данных.</summary>
    public IReadOnlyList<string> Diagnostics { get; } = diagnostics ?? [];
    /// <summary>Находит узел электрического вывода.</summary>
    public Net? Find(Guid component, string pin) => pins.GetValueOrDefault(new(component,pin));
    /// <summary>Проверяет наличие внешнего электрического вывода в узле.</summary>
    public bool Connected(Guid component,string pin) => Find(component,pin)?.Pins.Any(p=>p.ComponentId!=component)==true;
}
