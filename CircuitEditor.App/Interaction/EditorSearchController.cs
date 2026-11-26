using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using ICSharpCode.AvalonEdit;
namespace CircuitEditor.App.Interaction;
internal sealed class EditorSearchController(TextEditor editor,CodeViewModel model)
{
    internal void Handle(string action)
    {
        if(action=="FocusFind"||action=="FocusLine")
        {
            DependencyObject? parent=editor;
            while(parent is not null&&parent is not CodeView)parent=VisualTreeHelper.GetParent(parent);
            if(parent is CodeView view){view.UpdateLayout();if(view.FindName(action=="FocusFind"?"CodeFind":"CodeLine") is TextBox box){box.Focus();box.SelectAll();}}
        }
        else if(action=="FocusEditor")editor.Focus();
        else if(action=="FindNext")FindNext();
        else if(action=="FindPrevious")FindNext(backwards:true);
        else if(action=="ReplaceOne")ReplaceOne();
        else if(action=="ReplaceAll")ReplaceAll();
        else if(action=="GoLine")
        {
            if(!int.TryParse(model.GoToText,NumberStyles.None,CultureInfo.InvariantCulture,out var line)||line<1||line>editor.Document.LineCount)
                model.SearchMessage=$"Введите строку от 1 до {editor.Document.LineCount}.";
            else{Navigate(line,1);model.IsGoToOpen=false;}
        }
    }
    internal void Navigate(int line,int column)
    {
        line=Math.Clamp(line,1,editor.Document.LineCount);var target=editor.Document.GetLineByNumber(line);
        editor.CaretOffset=target.Offset+Math.Clamp(column-1,0,target.Length);editor.ScrollTo(line,column);editor.Focus();
    }
    internal bool FindNext(bool backwards=false)
    {
        if(string.IsNullOrEmpty(model.SearchText)){model.SearchMessage="Введите текст поиска.";return false;}
        var comparison=model.MatchCase?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase;
        var text=editor.Text;
        var start=Math.Min(editor.SelectionStart+editor.SelectionLength,text.Length);
        var index=backwards
            ? text.AsSpan(0,editor.SelectionStart).LastIndexOf(model.SearchText,comparison)
            : text.IndexOf(model.SearchText,start,comparison);
        if(index<0)index=backwards?text.LastIndexOf(model.SearchText,comparison):text.IndexOf(model.SearchText,comparison);
        if(index<0){model.SearchMessage="Совпадений нет.";return false;}
        editor.Select(index,model.SearchText.Length);editor.ScrollToLine(editor.Document.GetLineByOffset(index).LineNumber);
        model.SearchMessage="Совпадение найдено.";return true;
    }
    private void ReplaceOne()
    {
        var comparison=model.MatchCase?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase;
        if((editor.SelectionLength==0||!editor.SelectedText.Equals(model.SearchText,comparison))&&!FindNext())return;
        if(editor.Document.TextLength-editor.SelectionLength+(long)model.ReplacementText.Length>Core.Code.SketchDocument.MaximumTextLength)
        {model.SearchMessage="Результат замены превышает допустимый размер скетча.";return;}
        using(editor.Document.RunUpdate())editor.Document.Replace(editor.SelectionStart,editor.SelectionLength,model.ReplacementText);
        FindNext();
    }
    private void ReplaceAll()
    {
        if(model.SearchText.Length==0){model.SearchMessage="Введите текст поиска.";return;}
        var comparison=model.MatchCase?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase;var offsets=new List<int>();
        for(var start=0;start<=editor.Text.Length-model.SearchText.Length;)
        {var index=editor.Text.IndexOf(model.SearchText,start,comparison);if(index<0)break;offsets.Add(index);start=index+model.SearchText.Length;}
        if(editor.Document.TextLength+(long)offsets.Count*(model.ReplacementText.Length-model.SearchText.Length)>Core.Code.SketchDocument.MaximumTextLength)
        {model.SearchMessage="Результат замены превышает допустимый размер скетча.";return;}
        using(editor.Document.RunUpdate())foreach(var index in offsets.AsEnumerable().Reverse())editor.Document.Replace(index,model.SearchText.Length,model.ReplacementText);
        model.SearchMessage=$"Заменено: {offsets.Count}.";
    }
}
