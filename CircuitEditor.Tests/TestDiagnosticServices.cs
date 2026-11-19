using CircuitEditor.App.Services;
namespace CircuitEditor.Tests;
internal sealed class TestDiagnosticServices : IClipboardService,IErrorHelpService
{
    public string? Copied {get;private set;}
    public string? OpenedCode {get;private set;}
    public bool Busy {get;set;}
    public bool TryCopy(string text,out string? error){error=Busy?"занят":null;if(Busy)return false;Copied=text;return true;}
    public void Show(string? code=null)=>OpenedCode=code;
}
