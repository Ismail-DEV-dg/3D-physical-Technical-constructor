using CircuitEditor.Core.Code;
namespace CircuitEditor.App.ViewModels;
public sealed record CodeProblemViewModel(CodeProblem Problem)
{
    public string Text=>$"{Problem.Severity} · {Problem.Line}:{Problem.Column} · {Problem.Message}";
}
