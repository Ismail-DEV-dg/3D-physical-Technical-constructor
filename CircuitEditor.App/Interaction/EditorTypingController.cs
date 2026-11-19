using System.Windows.Input;
using CircuitEditor.Core.Code;
using ICSharpCode.AvalonEdit;
using CircuitEditor.App.ViewModels;
using System.Windows;
namespace CircuitEditor.App.Interaction;
internal sealed class EditorTypingController : IDisposable
{
    private readonly TextEditor editor;
    private readonly CodeViewModel model;
    private static readonly IReadOnlyDictionary<char,char> Pairs=new Dictionary<char,char>{{'(',')'},{'[',']'},{'{','}'},{'"','"'},{'\'','\''}};
    internal EditorTypingController(TextEditor editor,CodeViewModel model)
    {this.editor=editor;this.model=model;editor.TextArea.TextEntering+=Entering;editor.PreviewKeyDown+=KeyDown;DataObject.AddPastingHandler(editor,Pasting);}
    private void Entering(object? sender,TextCompositionEventArgs e)
    {
        if(e.Text.Length!=1){if(!Fits(e.Text.Length))e.Handled=true;return;}
        var ch=e.Text[0];var offset=editor.CaretOffset;var text=editor.Text;
        if(editor.SelectionLength==0&&offset<text.Length&&text[offset]==ch&&Pairs.Values.Contains(ch)
            && (IsCode(text,offset)||IsCode(text,offset+1)))
        {editor.CaretOffset++;e.Handled=true;return;}
        var paired=Pairs.TryGetValue(ch,out var close)&&IsCode(text,offset);
        if(!Fits(e.Text.Length+(paired?1+editor.SelectionLength:0))){e.Handled=true;return;}
        if(!paired)return;
        var selection=editor.SelectedText;var start=editor.SelectionStart;
        using(editor.Document.RunUpdate())editor.Document.Replace(start,editor.SelectionLength,ch+selection+close);
        editor.CaretOffset=start+selection.Length+1;e.Handled=true;
    }
    internal static bool IsCode(string text,int offset)=>CppTokenScanner.Scan(text[..offset]+" __EDITOR_CARET__")
        .LastOrDefault()?.Text=="__EDITOR_CARET__";
    private bool Fits(int inserted)
    {
        if(editor.Document.TextLength-editor.SelectionLength+(long)inserted<=SketchDocument.MaximumTextLength)return true;
        model.Output.Append($"Текст превышает ограничение {SketchDocument.MaximumTextLength} символов; вставка отменена.");return false;
    }
    private void Pasting(object sender,DataObjectPastingEventArgs e)
    {
        var text=e.DataObject.GetData(DataFormats.UnicodeText) as string ?? e.DataObject.GetData(DataFormats.Text) as string;
        if(text is not null&&!Fits(text.Length))e.CancelCommand();
    }
    private void KeyDown(object sender,KeyEventArgs e)
    {
        if(Keyboard.Modifiers!=ModifierKeys.None||editor.SelectionLength>0)return;
        var offset=editor.CaretOffset;var text=editor.Text;
        if(offset==0||offset>=text.Length||!Pairs.TryGetValue(text[offset-1],out var close)||text[offset]!=close||!IsCode(text,offset-1))return;
        if(e.Key==Key.Back){editor.Document.Remove(offset-1,2);e.Handled=true;}
        else if(e.Key==Key.Enter&&text[offset-1]=='{')
        {
            var line=editor.Document.GetLineByOffset(offset);var prefix=editor.Document.GetText(line.Offset,offset-line.Offset);
            var indent=new string(prefix.TakeWhile(char.IsWhiteSpace).ToArray());var newline=text.Contains("\r\n",StringComparison.Ordinal)?"\r\n":"\n";
            if(!Fits(newline.Length*2+indent.Length*2+2)){e.Handled=true;return;}
            editor.Document.Insert(offset,newline+indent+"  "+newline+indent);editor.CaretOffset=offset+newline.Length+indent.Length+2;e.Handled=true;
        }
    }
    public void Dispose(){editor.TextArea.TextEntering-=Entering;editor.PreviewKeyDown-=KeyDown;DataObject.RemovePastingHandler(editor,Pasting);}
}
