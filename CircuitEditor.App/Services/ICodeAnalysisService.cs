using CircuitEditor.Core.Code;
using CircuitEditor.Core.Models;
namespace CircuitEditor.App.Services;
public interface ICodeAnalysisService
{
    Task<IReadOnlyList<CodeProblem>> AnalyzeAsync(CircuitProject snapshot,SketchDocument sketch,CancellationToken cancellationToken);
}
