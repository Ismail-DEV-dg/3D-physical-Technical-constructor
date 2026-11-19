using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class OutputViewModel : ObservableObject
{
    private const int MaximumLines=500;
    public ObservableCollection<string> Lines {get;}=[];
    [ObservableProperty] private bool autoScroll=true;
    public void Append(string text)
    {
        foreach(var line in text.Replace("\r","").Split('\n'))
        {Lines.Add($"{DateTime.Now:HH:mm:ss}  {line}");while(Lines.Count>MaximumLines)Lines.RemoveAt(0);}
    }
    [RelayCommand] private void Clear()=>Lines.Clear();
}
