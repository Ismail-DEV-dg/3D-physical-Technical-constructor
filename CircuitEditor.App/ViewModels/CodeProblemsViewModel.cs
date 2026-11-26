using CircuitEditor.Core.Code;
using CommunityToolkit.Mvvm.ComponentModel;
namespace CircuitEditor.App.ViewModels;
public sealed partial class CodeProblemsViewModel : ObservableObject
{
    private IReadOnlyList<CodeProblem> analysis=[];
    public IReadOnlyList<CodeProblemViewModel> Items=>analysis.Distinct().Select(p=>new CodeProblemViewModel(p)).ToArray();
    public string Summary=>$"Проблем: {Items.Count} · ограниченный анализ Arduino API";
    [ObservableProperty] private CodeProblemViewModel? selected;
    public event EventHandler<CodeProblem>? NavigateRequested;
    partial void OnSelectedChanged(CodeProblemViewModel? value){if(value is not null)NavigateRequested?.Invoke(this,value.Problem);}
    public void SetAnalysis(IReadOnlyList<CodeProblem> value){analysis=value;Notify();}
    public void Clear(){analysis=[];Selected=null;Notify();}
    private void Notify(){OnPropertyChanged(nameof(Items));OnPropertyChanged(nameof(Summary));}
}
