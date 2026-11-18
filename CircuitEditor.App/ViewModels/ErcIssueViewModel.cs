using CircuitEditor.Core.Validation;
namespace CircuitEditor.App.ViewModels;
public sealed record ErcIssueViewModel(ErcIssue Issue)
{
    public string Icon=>Issue.Severity switch {ErcSeverity.Error=>"✕",ErcSeverity.Warning=>"!",_=>"i"};
    public string Text=>$"{Icon} [{Issue.Code}] {Issue.Message}";
    public string Color=>Issue.Severity==ErcSeverity.Error ? "#FF3B5C" : Issue.Severity==ErcSeverity.Warning ? "#FFD633" : "#00E5FF";
}
