using System.Windows.Input;
namespace CircuitEditor.App.Interaction;
public sealed record ShortcutBinding(string Id,Key Key,ModifierKeys Modifiers,string Keys,string Group,string Description,bool CanvasOnly=false,bool AllowText=false)
{
    public string Scope=>CanvasOnly?"На холсте":AllowText?"В главном окне, включая поля ввода":"В главном окне, вне полей ввода";
}
