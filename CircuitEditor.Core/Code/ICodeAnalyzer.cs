using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Code;
/// <summary>Ограниченный статический анализ Arduino API без исполнения C++.</summary>
public interface ICodeAnalyzer
{
    /// <summary>Сравнивает текст и электрические подключения снимка проекта.</summary>
    IReadOnlyList<CodeProblem> Analyze(CircuitProject project,Netlist.Netlist nets,SketchDocument sketch);
}
