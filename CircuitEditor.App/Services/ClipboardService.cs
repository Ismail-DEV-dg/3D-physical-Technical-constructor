using System.Runtime.InteropServices;
using System.Windows;
namespace CircuitEditor.App.Services;
public sealed class ClipboardService : IClipboardService
{
    public bool TryCopy(string text,out string? error)
    {
        try{Clipboard.SetDataObject(text,true);error=null;return true;}
        catch(Exception e) when(e is ExternalException or InvalidOperationException or ThreadStateException)
        {error="Не удалось скопировать: буфер обмена занят. Выделите текст и нажмите Ctrl+C либо повторите попытку.";return false;}
    }
}
