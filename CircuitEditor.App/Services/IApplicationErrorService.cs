namespace CircuitEditor.App.Services;

public interface IApplicationErrorService
{
    void Report(Exception error);
}
