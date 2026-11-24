using System.Threading.Channels;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Tests;

internal sealed class TestCodeAnalysisService : ICodeAnalysisService
{
    private readonly Channel<(SketchDocument Sketch, TaskCompletionSource<IReadOnlyList<CodeProblem>> Completion)> requests =
        Channel.CreateUnbounded<(SketchDocument, TaskCompletionSource<IReadOnlyList<CodeProblem>>)>();

    public Task<IReadOnlyList<CodeProblem>> AnalyzeAsync(CircuitProject snapshot, SketchDocument sketch, CancellationToken cancellationToken)
    {
        // Intentionally ignore cancellation so the test can finish an outdated request.
        var completion = new TaskCompletionSource<IReadOnlyList<CodeProblem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        requests.Writer.TryWrite((sketch, completion));
        return completion.Task;
    }

    public ValueTask<(SketchDocument Sketch, TaskCompletionSource<IReadOnlyList<CodeProblem>> Completion)> NextRequestAsync(
        CancellationToken timeout) => requests.Reader.ReadAsync(timeout);

    public bool HasPendingRequests => requests.Reader.TryPeek(out _);
}
