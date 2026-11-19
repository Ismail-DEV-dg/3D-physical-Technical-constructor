using CircuitEditor.App.Services;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Models;
namespace CircuitEditor.Tests;
internal sealed class TestCodeAnalysisService : ICodeAnalysisService
{
    public List<(SketchDocument Sketch,TaskCompletionSource<IReadOnlyList<CodeProblem>> Completion)> Requests {get;}=[];
    public Task<IReadOnlyList<CodeProblem>> AnalyzeAsync(CircuitProject snapshot,SketchDocument sketch,CancellationToken cancellationToken)
    {
        var completion=new TaskCompletionSource<IReadOnlyList<CodeProblem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        Requests.Add((sketch,completion));return completion.Task;
    }
}
