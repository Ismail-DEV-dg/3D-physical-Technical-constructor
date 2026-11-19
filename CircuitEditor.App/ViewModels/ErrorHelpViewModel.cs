using CircuitEditor.App.Services;
using CircuitEditor.Core.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ErrorHelpViewModel(IErrorDocumentationService documentation,IClipboardService clipboard) : ObservableObject
{
    [ObservableProperty] private string search="";
    [ObservableProperty] private DiagnosticArticle? selected;
    [ObservableProperty] private string feedback="";
    public IReadOnlyList<DiagnosticArticle> Articles=>documentation.Articles.Where(a=>Search.Contains(a.Code,StringComparison.OrdinalIgnoreCase)
        || Search.Split(' ',StringSplitOptions.RemoveEmptyEntries).All(term=>(a.Code+" "+a.Title+" "+a.Cause+" "+a.Fix+" "+string.Join(" ",a.Keywords)).Contains(term,StringComparison.OrdinalIgnoreCase))).ToArray();
    public string CountSummary=>$"Найдено статей: {Articles.Count}";
    public string ArticleText=>Selected is { } a ? $"{a.Title}\nКод: {a.Code}\n\nПочему возникает\n{a.Cause}\n\nКак исправить\n{a.Fix}" : "По запросу ничего не найдено. Попробуйте «маркеры», «LED», «GPIO34», код ошибки или название компонента.";
    partial void OnSearchChanged(string value)
    {
        OnPropertyChanged(nameof(Articles));OnPropertyChanged(nameof(CountSummary));
        if(Selected is null||!Articles.Contains(Selected))Selected=Articles.FirstOrDefault();OnPropertyChanged(nameof(ArticleText));
    }
    partial void OnSelectedChanged(DiagnosticArticle? value)=>OnPropertyChanged(nameof(ArticleText));
    public void SelectCode(string? code)
    {Search="";Selected=code is null?Articles.FirstOrDefault():documentation.Resolve(code);}
    public event EventHandler? CloseRequested;
    public event EventHandler? FocusSearchRequested;
    [RelayCommand] private void Close()=>CloseRequested?.Invoke(this,EventArgs.Empty);
    [RelayCommand] private void FocusSearch()=>FocusSearchRequested?.Invoke(this,EventArgs.Empty);
    [RelayCommand] private void Copy()=>Feedback=clipboard.TryCopy(ArticleText,out var error)?"Статья скопирована.":error??"Не удалось скопировать.";
}
