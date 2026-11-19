using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Code;
/// <summary>Генерация ограниченных скетчей по общей электрической модели.</summary>
public interface ICodeGenerator
{
    /// <summary>Обновляет только управляемые секции; при ошибке возвращает исходный текст.</summary>
    CodeGenerationResult Generate(CircuitProject project, Netlist.Netlist nets, SketchDocument sketch);
}
