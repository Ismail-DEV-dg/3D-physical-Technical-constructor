using System.Windows;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
namespace CircuitEditor.App.Services;
public sealed class CalculatorService(CalculatorViewModel viewModel) : ICalculatorService
{
    public void Show()
    {
        viewModel.LoadSelectedLed();
        var window=new CalculatorWindow {Owner=Application.Current.MainWindow,DataContext=viewModel};
        void Close(object? sender,EventArgs e)=>window.Close();
        viewModel.CloseRequested+=Close;
        try{window.ShowDialog();}finally{viewModel.CloseRequested-=Close;}
    }
}
