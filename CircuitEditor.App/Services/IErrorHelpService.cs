namespace CircuitEditor.App.Services;
public interface IErrorHelpService
{
    void Show(string? code=null);
    void ShowSection(string section) => Show();
}
