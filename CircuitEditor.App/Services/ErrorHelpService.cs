using System.Windows;
using System.Windows.Controls;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
namespace CircuitEditor.App.Services;
public sealed class ErrorHelpService(IErrorDocumentationService documentation,IClipboardService clipboard) : IErrorHelpService
{
    public void Show(string? code=null)
    {
        var vm=new ErrorHelpViewModel(documentation,clipboard);vm.SelectCode(code);
        var owner=Application.Current.Windows.OfType<Window>().FirstOrDefault(w=>w.IsActive)??Application.Current.MainWindow;
        var window=new ErrorHelpWindow {Owner=owner,DataContext=vm};
        void Close(object? sender,EventArgs e)=>window.Close();
        void Focus(object? sender,EventArgs e){((TextBox)window.FindName("ErrorSearch")).Focus();((TextBox)window.FindName("ErrorSearch")).SelectAll();}
        vm.CloseRequested+=Close;vm.FocusSearchRequested+=Focus;window.Loaded+=Focus;
        try{window.ShowDialog();}finally{vm.CloseRequested-=Close;vm.FocusSearchRequested-=Focus;window.Loaded-=Focus;}
    }
}
