using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CircuitEditor.App.ViewModels;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Indentation.CSharp;
using ICSharpCode.AvalonEdit.Document;
namespace CircuitEditor.App.Interaction;
internal sealed class CodeEditorSession : IDisposable
{
    private const int MinimumFontSize=10,MaximumFontSize=26,IndentationSize=2;
    private readonly TextEditor editor;
    private readonly CodeViewModel model;
    private EditorDocumentState state;
    private readonly EditorFoldingController folding;
    private readonly EditorTypingController typing;
    private readonly EditorCompletionController completion;
    private readonly EditorSearchController search;
    private readonly EditorLineCommands lines;
    private bool syncing;
    private UndoStack undo;
    private readonly KeyBinding redoBinding=new(ApplicationCommands.Redo,Key.Z,ModifierKeys.Control|ModifierKeys.Shift);
    internal CodeEditorSession(TextEditor editor,CodeViewModel model)
    {
        this.editor=editor;this.model=model;state=EditorDocumentState.Get(model);editor.Document=state.Document;
        editor.Options.IndentationSize=IndentationSize;editor.Options.ConvertTabsToSpaces=true;
        editor.Options.HighlightCurrentLine=true;
        editor.TextArea.IndentationStrategy=new CSharpIndentationStrategy(editor.Options);
        folding=new(editor);typing=new(editor,model);completion=new(editor,model);search=new(editor,model);lines=new(editor,model);
        EditorHighlighting.Apply(editor);editor.CaretOffset=Math.Min(state.CaretOffset,editor.Document.TextLength);
        editor.TextChanged+=TextChanged;editor.TextArea.Caret.PositionChanged+=CaretChanged;editor.PreviewMouseWheel+=Wheel;
        model.PropertyChanged+=ModelChanged;model.EditorActionRequested+=ActionRequested;
        undo=editor.Document.UndoStack;undo.PropertyChanged+=UndoChanged;editor.InputBindings.Add(redoBinding);
        Navigate();CaretChanged(this,EventArgs.Empty);UndoChanged(this,new PropertyChangedEventArgs(null));model.RequestAnalysis();
    }
    private void TextChanged(object? sender,EventArgs e)
    {
        if(!syncing)model.Text=editor.Text;
    }
    private void ModelChanged(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(model.DocumentVersion))
        {undo.PropertyChanged-=UndoChanged;state=EditorDocumentState.Get(model);editor.Document=state.Document;undo=editor.Document.UndoStack;undo.PropertyChanged+=UndoChanged;folding.Refresh();UndoChanged(this,e);return;}
        if(e.PropertyName==nameof(model.Text)&&editor.Text!=model.Text)
        {
            syncing=true;
            try{using(editor.Document.RunUpdate())editor.Document.Replace(0,editor.Document.TextLength,model.Text);}
            finally{syncing=false;}
        }
        if(e.PropertyName==nameof(model.ThemeVersion))EditorHighlighting.Apply(editor);
        if(e.PropertyName==nameof(model.NavigationVersion))Navigate();
    }
    private void Navigate()
    {
        if(state.NavigationVersion==model.NavigationVersion)return;
        state.NavigationVersion=model.NavigationVersion;
        search.Navigate(model.RequestedLine,model.RequestedColumn);
    }
    private void CaretChanged(object? sender,EventArgs e)
    {state.CaretOffset=editor.CaretOffset;model.CursorLine=editor.TextArea.Caret.Line;model.CursorColumn=editor.TextArea.Caret.Column;}
    private void ActionRequested(object? sender,string action)
    {
        switch (action)
        {
            case "Undo": editor.Undo(); break;
            case "Redo": editor.Redo(); break;
            case "Copy": editor.Copy(); break;
            case "Paste": editor.Paste(); break;
            case "Completion": completion.Open(true); break;
            case "DuplicateLine": completion.Dismiss(); lines.Duplicate(); break;
            case "MoveLineUp": completion.Dismiss(); lines.Move(-1); break;
            case "MoveLineDown": completion.Dismiss(); lines.Move(1); break;
            case "DeleteLine": completion.Dismiss(); lines.Delete(); break;
            case "ToggleComment": completion.Dismiss(); lines.ToggleComment(); break;
            case "FocusEditor": completion.Dismiss(); search.Handle(action); break;
            default: search.Handle(action); break;
        }
    }
    private void UndoChanged(object? sender,PropertyChangedEventArgs e){model.CanUndo=editor.CanUndo;model.CanRedo=editor.CanRedo;}
    private void Wheel(object sender,MouseWheelEventArgs e)
    {
        if(Keyboard.Modifiers!=ModifierKeys.Control)return;
        model.EditorFontSize=Math.Clamp(model.EditorFontSize+(e.Delta>0?1:-1),MinimumFontSize,MaximumFontSize);e.Handled=true;
    }
    public void Dispose()
    {
        model.PropertyChanged-=ModelChanged;model.EditorActionRequested-=ActionRequested;
        undo.PropertyChanged-=UndoChanged;editor.InputBindings.Remove(redoBinding);
        editor.TextChanged-=TextChanged;editor.TextArea.Caret.PositionChanged-=CaretChanged;editor.PreviewMouseWheel-=Wheel;
        folding.Dispose();typing.Dispose();completion.Dispose();
    }
}
