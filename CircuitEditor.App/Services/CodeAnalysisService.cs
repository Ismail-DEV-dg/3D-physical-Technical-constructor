using CircuitEditor.Core.Code;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
namespace CircuitEditor.App.Services;
public sealed class CodeAnalysisService(ICodeAnalyzer analyzer,INetlistBuilder builder) : ICodeAnalysisService
{
    public Task<IReadOnlyList<CodeProblem>> AnalyzeAsync(CircuitProject snapshot,SketchDocument sketch,CancellationToken cancellationToken)=>Task.Run(()=>
    {
        cancellationToken.ThrowIfCancellationRequested();var nets=builder.Build(snapshot);
        var result=analyzer.Analyze(snapshot,nets,sketch);cancellationToken.ThrowIfCancellationRequested();return result;
    },cancellationToken);
}
