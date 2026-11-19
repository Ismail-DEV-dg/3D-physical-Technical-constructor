using System.ComponentModel;
using CircuitEditor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class DiagnosticsViewModel : ObservableObject,IDisposable
{
    private readonly ProjectAnalysisViewModel? analysis;
    private readonly CodeViewModel code;
    private readonly IClipboardService? clipboard;
    private readonly IErrorDocumentationService? documentation;
    private readonly IErrorHelpService? help;
    private IReadOnlyList<DiagnosticItemViewModel> all=[];
    public DiagnosticsViewModel(ProjectAnalysisViewModel? analysis,CodeViewModel code,IClipboardService? clipboard=null,
        IErrorDocumentationService? documentation=null,IErrorHelpService? help=null)
    {
        this.analysis=analysis;this.code=code;this.clipboard=clipboard;this.documentation=documentation;this.help=help;
        if(analysis is not null){analysis.PropertyChanged+=Changed;analysis.Results.PropertyChanged+=Changed;}code.ProblemsPanel.PropertyChanged+=Changed;Rebuild();
    }
    [ObservableProperty] private string search="";
    [ObservableProperty] private string source="Все";
    [ObservableProperty] private string severity="Все";
    [ObservableProperty] private string feedback="";
    [ObservableProperty] private DiagnosticItemViewModel? selected;
    public IReadOnlyList<string> Sources {get;}=["Все","Схема","Код","Расчёт"];
    public IReadOnlyList<string> Severities {get;}=["Все","Ошибки","Предупреждения","Информация"];
    public IReadOnlyList<DiagnosticItemViewModel> Items=>all.Where(i=>(Source=="Все"||i.Source==Source)&&(Severity=="Все"||i.Severity==(Severity switch {"Ошибки"=>"Error","Предупреждения"=>"Warning",_=>"Info"}))
        && Search.Split(' ',StringSplitOptions.RemoveEmptyEntries).All(term=>i.FullText.Contains(term,StringComparison.OrdinalIgnoreCase))).ToArray();
    public string Summary=>$"Показано {Items.Count} из {all.Count} · ошибок {all.Count(i=>i.Severity=="Error")} · предупреждений {all.Count(i=>i.Severity=="Warning")}";
    public bool IsEmpty=>Items.Count==0;
    partial void OnSearchChanged(string value)=>Notify();
    partial void OnSourceChanged(string value)=>Notify();
    partial void OnSeverityChanged(string value)=>Notify();
    partial void OnSelectedChanged(DiagnosticItemViewModel? value){if(value is not null)value.NavigateCommand.Execute(null);}
    private void Changed(object? sender,PropertyChangedEventArgs e)
    {if(e.PropertyName is nameof(ProjectAnalysisViewModel.FilteredIssues) or nameof(CodeProblemsViewModel.Items) or nameof(CalculationResultsViewModel.Result))Rebuild();}
    private void Rebuild()
    {
        var rows=new List<DiagnosticItemViewModel>();
        foreach(var issue in analysis?.AllIssues??[])
            rows.Add(new("Схема",issue.Issue.Code,issue.Issue.Message,issue.Issue.Severity.ToString(),()=>analysis!.SelectedIssue=issue,clipboard,documentation,help));
        foreach(var issue in code.ProblemsPanel.Items)
            rows.Add(new("Код",issue.Problem.Code,$"Строка {issue.Problem.Line}:{issue.Problem.Column} · {issue.Problem.Message}",issue.Problem.Severity,
                ()=>code.RevealProblem(issue.Problem),clipboard,documentation,help));
        foreach(var message in analysis?.Results.Result?.Diagnostics??[])
        {
            var id=message.Contains("Плавающий",StringComparison.OrdinalIgnoreCase)?"DC_FLOATING":message.Contains("не поддерж",StringComparison.OrdinalIgnoreCase)?"DC_UNSUPPORTED":"GENERAL";
            rows.Add(new("Расчёт",id,message,"Warning",()=>analysis!.ShowResults(),clipboard,documentation,help));
        }
        all=rows.OrderBy(i=>i.Severity=="Error"?0:i.Severity=="Warning"?1:2).ToArray();Selected=null;Notify();
    }
    private void Notify(){if(Selected is not null&&!Items.Contains(Selected))Selected=null;OnPropertyChanged(nameof(Items));OnPropertyChanged(nameof(Summary));OnPropertyChanged(nameof(IsEmpty));}
    [RelayCommand] private void CopySelected(){if(Selected is { } item)item.CopyCommand.Execute(null);}
    [RelayCommand] private void CopyAll()
    {Feedback=clipboard?.TryCopy(string.Join(Environment.NewLine,Items.Select(i=>i.FullText)),out _)==true?"Список скопирован.":"Выберите сообщение и скопируйте Ctrl+C.";}
    public void Dispose(){if(analysis is not null){analysis.PropertyChanged-=Changed;analysis.Results.PropertyChanged-=Changed;}code.ProblemsPanel.PropertyChanged-=Changed;}
}
