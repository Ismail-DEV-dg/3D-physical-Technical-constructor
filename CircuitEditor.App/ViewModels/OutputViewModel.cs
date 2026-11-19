using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CircuitEditor.App.Services;
namespace CircuitEditor.App.ViewModels;
public sealed partial class OutputViewModel(IClipboardService? clipboard=null) : ObservableObject
{
    private const int MaximumLines=500;
    private string? lastMessage;
    public ObservableCollection<string> Lines {get;}=[];
    [ObservableProperty] private bool autoScroll=true;
    [ObservableProperty] private string? selectedLine;
    [ObservableProperty] private string feedback="";
    [RelayCommand] private void CopySelected()=>Feedback=clipboard?.TryCopy(SelectedLine??string.Join(Environment.NewLine,Lines),out _)==true?"Скопировано.":"Выберите текст и нажмите Ctrl+C.";
    public void Append(string text)
    {
        if(text==lastMessage)return;lastMessage=text;
        foreach(var line in text.Replace("\r","").Split('\n'))
        {Lines.Add($"{DateTime.Now:HH:mm:ss}  {line}");while(Lines.Count>MaximumLines)Lines.RemoveAt(0);}
    }
    [RelayCommand] private void Clear(){Lines.Clear();lastMessage=null;}
}
