namespace CircuitEditor.Core.Netlist;
/// <summary>Адрес электрического вывода (p), физического контакта (c) или отверстия (h).</summary>
public sealed record NetTerminal(string Kind, Guid OwnerId, string TerminalId)
{
    /// <summary>Детерминированный ключ вершины.</summary>
    public string Key => $"{Kind}:{OwnerId:N}:{TerminalId}";
}
