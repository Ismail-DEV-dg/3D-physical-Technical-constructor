using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Layout;

/// <summary>Сервис вычисления геометрии компонента по измеренному содержимому.</summary>
public interface IComponentLayoutCalculator
{
    /// <summary>Строит раскладку текущего обозначения, названия, номинала и адреса.</summary>
    ComponentLayout Calculate(Component component);
}
