using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Netlist;
/// <summary>Узел идеальной связности; нагрузки узлы не объединяют.</summary>
public sealed record Net(string Id, string Name, IReadOnlyList<NetTerminal> Terminals, IReadOnlyList<PinReference> Pins);
