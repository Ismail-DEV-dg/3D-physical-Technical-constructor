using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Netlist;
/// <summary>Построение связности без UI и без расчёта сопротивлений.</summary>
public interface INetlistBuilder
{
    /// <summary>Создаёт снимок узлов проекта.</summary>
    Netlist Build(CircuitProject project);
}
