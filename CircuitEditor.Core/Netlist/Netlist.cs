using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.Core.Netlist;
/// <summary>Неизменяемый результат построения графа.</summary>
public sealed class Netlist(IReadOnlyList<Net> nets, IReadOnlyList<string>? diagnostics = null)
{
    private readonly Dictionary<PinReference,Net> pins=nets.SelectMany(n=>n.Pins.Select(p=>(p,n))).ToDictionary(x=>x.p,x=>x.n);
    private readonly Dictionary<string, Net> terminals = nets.SelectMany(n => n.Terminals
        .Select(t => (t.Key, n))).ToDictionary(p => p.Key, p => p.n);
    /// <summary>Все узлы, включая свободные контакты breadboard.</summary>
    public IReadOnlyList<Net> Nets { get; } = nets;
    /// <summary>Ошибки исходных данных.</summary>
    public IReadOnlyList<string> Diagnostics { get; } = diagnostics ?? [];
    /// <summary>Находит узел электрического вывода.</summary>
    public Net? Find(Guid component, string pin) => pins.GetValueOrDefault(new(component,pin));
    /// <summary>Находит узел физического контакта или отверстия.</summary>
    public Net? Find(WireEndpoint endpoint) => terminals.GetValueOrDefault(new NetTerminal(
        endpoint.Kind == WireEndpointKind.ComponentContact ? "c" : "h", endpoint.OwnerId, endpoint.TerminalId).Key);
    /// <summary>Проверяет наличие внешнего электрического вывода в узле.</summary>
    public bool Connected(Guid component,string pin) => Find(component,pin)?.Pins.Any(p=>p.ComponentId!=component)==true;
}
