namespace CircuitEditor.App.Services;
public interface IClipboardService
{
    bool TryCopy(string text,out string? error);
}
