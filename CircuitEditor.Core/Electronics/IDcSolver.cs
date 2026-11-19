using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Electronics;
/// <summary>Статический DC-расчёт; не симуляция во времени.</summary>
public interface IDcSolver
{
    /// <summary>Возвращает напряжения, токи и диагностику без UI.</summary>
    DcResult Solve(CircuitProject project,Netlist.Netlist nets);
    /// <summary>Рассчитывает цепь с дополнительными ветвями сеанса.</summary>
    DcResult Solve(CircuitProject project, Netlist.Netlist nets, IReadOnlyList<DcBranch> additional,
        DcMatrixCache cache, IReadOnlySet<Guid>? modeledComponents = null);
}
