using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CircuitEditor.App.Services;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ChoiceDialogViewModel : ObservableObject
{
    public ChoiceDialogViewModel(string title,string message,IReadOnlyList<string> choices,Action<string> choose,
        IClipboardService? clipboard=null,IErrorHelpService? help=null,IErrorDocumentationService? documentation=null,Action? dismiss=null)
    {
        Title=title;Message=message;Choices=choices;
        DismissCommand=new RelayCommand(()=>dismiss?.Invoke());
        ChooseCommand=new RelayCommand<string>(s=>{if(s is not null)choose(s);});
        CopyCommand=new RelayCommand(()=>Feedback=clipboard?.TryCopy(message,out _)==true?"Сообщение скопировано.":"Выделите текст и нажмите Ctrl+C.");
        HelpCommand=new RelayCommand(()=>help?.Show(documentation?.Resolve("GENERAL",message).Code));
    }
    public string Title {get;}
    public string Message {get;}
    public IReadOnlyList<string> Choices {get;}
    public bool IsDiagnostic=>Title is "Ошибка" or "Предупреждение";
    [ObservableProperty] private string feedback="";
    public IRelayCommand<string> ChooseCommand {get;}
    public IRelayCommand DismissCommand {get;}
    public IRelayCommand CopyCommand {get;}
    public IRelayCommand HelpCommand {get;}
}
