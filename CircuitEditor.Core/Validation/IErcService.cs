using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
namespace CircuitEditor.Core.Validation;
/// <summary>Статическая проверка электрического проекта.</summary>
public interface IErcService
{
    /// <summary>Выполняет зарегистрированные правила.</summary>
    IReadOnlyList<ErcIssue> Check(CircuitProject project, Netlist.Netlist nets);
}
