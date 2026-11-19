using System.Windows;
using System.Windows.Controls;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.App.Services;

public sealed class ErrorHelpService(IErrorDocumentationService documentation, IClipboardService clipboard, IGuideRepository guides)
    : IErrorHelpService
{
    public void Show(string? code = null) => Open(code is null ? "home" : "errors", code);
    public void ShowSection(string section) => Open(section, null);

    private void Open(string section, string? code)
    {
        var catalog = guides.Load();
        var model = new ErrorHelpViewModel(documentation, clipboard, catalog.Value, catalog.Error);
        if (code is not null) model.SelectCode(code);
        else model.OpenSection(section);
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
        var window = new ErrorHelpWindow { Owner = owner, DataContext = model };
        void Close(object? sender, EventArgs e) => window.Close();
        void Focus(object? sender, EventArgs e)
        {
            var search = model.IsErrors ? window.FindName("ErrorSearch") as TextBox :
                model.IsGuide ? (window.FindName("GuideBrowser") as GuideBrowserView)?.FindName("GuideSearch") as TextBox : null;
            search?.Focus();
            search?.SelectAll();
        }
        model.CloseRequested += Close;
        model.FocusSearchRequested += Focus;
        window.Loaded += Focus;
        try { window.ShowDialog(); }
        finally
        {
            model.CloseRequested -= Close;
            model.FocusSearchRequested -= Focus;
            window.Loaded -= Focus;
        }
    }
}
