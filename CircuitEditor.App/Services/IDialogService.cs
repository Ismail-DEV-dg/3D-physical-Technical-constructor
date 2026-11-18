namespace CircuitEditor.App.Services;
public interface IDialogService
{
    void ShowError(string message);
    void ShowWarning(string message);
    string? OpenProject();
    string? SaveFile(string filter, string extension, string? suggested = null);
    SaveDecision ConfirmUnsaved();
    bool ConfirmRecovery();
    double? ChooseExportScale();
}
