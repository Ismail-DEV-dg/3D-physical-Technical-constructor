namespace CircuitEditor.App.Services;

public interface IApplicationErrorService
{
    void SetRecoveryAction(Func<bool> saveCopy);
    void Report(Exception error);
}
