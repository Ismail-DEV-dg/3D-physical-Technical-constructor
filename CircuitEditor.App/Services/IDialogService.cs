namespace CircuitEditor.App.Services;
public interface IDialogService
{
    string? OpenProject();
    string? SaveFile(string filter, string extension, string? suggested = null);
    SaveDecision ConfirmUnsaved();
    bool ConfirmRecovery();
    double? ChooseExportScale();
}
