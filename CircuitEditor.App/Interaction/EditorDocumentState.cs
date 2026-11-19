using System.Runtime.CompilerServices;
using CircuitEditor.App.ViewModels;
using ICSharpCode.AvalonEdit.Document;
namespace CircuitEditor.App.Interaction;
internal sealed class EditorDocumentState
{
    private static readonly ConditionalWeakTable<CodeViewModel,EditorDocumentState> Documents=new();
    internal TextDocument Document {get;private set;}=new();
    internal int Version {get;private set;}=-1;
    internal int CaretOffset {get;set;}
    internal int NavigationVersion {get;set;}
    internal static EditorDocumentState Get(CodeViewModel model)
    {
        var state=Documents.GetOrCreateValue(model);
        if(state.Version!=model.DocumentVersion)
        {state.Version=model.DocumentVersion;state.Document=new TextDocument(model.Text);state.CaretOffset=0;state.NavigationVersion=0;}
        return state;
    }
}
