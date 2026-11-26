using System.Windows.Input;
namespace CircuitEditor.App.Interaction;
public sealed record ShortcutBinding(string Id,Key Key,ModifierKeys Modifiers,string Keys,string Group,string Description,bool CanvasOnly=false,bool AllowText=false,
    bool CodeOnly=false,bool EditorOnly=false,bool OutsideCode=false)
{
    public string Scope => EditorOnly ? "В тексте скетча" : CodeOnly ? "Во вкладке «Код»" :
        CanvasOnly ? "На холсте" : OutsideCode ? "Вне вкладки «Код»" :
        AllowText ? "В главном окне, включая поля ввода" : "В главном окне, вне полей ввода";

    public bool CanCustomize => Key is not (Key.None or Key.Escape);

    public bool MatchesContext(bool editing, bool canvas, bool code, bool editor) =>
        (!CanvasOnly || canvas) && (!CodeOnly || code) && (!EditorOnly || editor) &&
        (!OutsideCode || !code) && (!editing || AllowText || CodeOnly);
}
