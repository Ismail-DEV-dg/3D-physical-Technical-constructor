namespace CircuitEditor.App.Services;
public interface IDialogService
{
    void ShowError(string message);
    bool OfferCrashCopy(bool journalWritten) => false;
    void ShowWarning(string message);
    string? OpenProject();
    string? OpenFile(string filter) => null;
    string? SaveFile(string filter, string extension, string? suggested = null);
    SaveDecision ConfirmUnsaved();
    bool ConfirmRecovery();
    bool ConfirmDeleteChip(string name) => false;
    bool UseLocalChipVersion(string name, int embeddedRevision, int localRevision) => false;
    double? ChooseExportScale();
    void ShowAbout() { }
}
