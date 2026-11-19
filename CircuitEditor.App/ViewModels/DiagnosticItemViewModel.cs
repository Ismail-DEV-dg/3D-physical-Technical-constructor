using CircuitEditor.App.Services;
using CircuitEditor.Core.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class DiagnosticItemViewModel : ObservableObject
{
    public DiagnosticItemViewModel(string source,string code,string message,string severity,Action navigate,
        IClipboardService? clipboard,IErrorDocumentationService? documentation,IErrorHelpService? help)
    {
        Source=source;Code=code;Message=message;Severity=severity;
        Article=documentation?.Resolve(code,message)??new(code,"Как исправить","Проверьте сообщение.","Откройте справку по ошибкам и найдите код сообщения.",[]);
        NavigateCommand=new RelayCommand(navigate);HelpCommand=new RelayCommand(()=>help?.Show(Article.Code));
        CopyCommand=new RelayCommand(()=>Feedback=clipboard?.TryCopy(FullText,out _)==true?"Скопировано":"Выделите текст и нажмите Ctrl+C.");
    }
    public string Source {get;}
    public string Code {get;}
    public string Message {get;}
    public string Severity {get;}
    public string Color=>Severity=="Error"?"#FF3B5C":Severity=="Warning"?"#D5A62D":"#00A5C0";
    public string Caption=>$"{Source} · {Severity switch {"Error"=>"Ошибка","Warning"=>"Предупреждение",_=>"Информация"}} · {Code}";
    public string FullText=>$"[{Source}] [{Severity}] [{Code}] {Message}";
    public DiagnosticArticle Article {get;}
    [ObservableProperty] private string feedback="";
    public IRelayCommand NavigateCommand {get;}
    public IRelayCommand HelpCommand {get;}
    public IRelayCommand CopyCommand {get;}
}
