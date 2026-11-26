using System.Windows;
using System.Windows.Controls;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
namespace CircuitEditor.App.Services;
public sealed class ShortcutGuideService(IShortcutSettings settings) : IShortcutGuideService
{
    public void Show()
    {
        var vm=new ShortcutGuideViewModel(settings);var window=new ShortcutGuideWindow{Owner=Application.Current.MainWindow,DataContext=vm};
        void Close(object? sender,EventArgs e)=>window.Close();
        vm.CloseRequested+=Close;
        window.Loaded+=(_,_)=>((TextBox)window.FindName("HelpSearch")).Focus();
        try{window.ShowDialog();}finally{vm.CloseRequested-=Close;}
    }
}
