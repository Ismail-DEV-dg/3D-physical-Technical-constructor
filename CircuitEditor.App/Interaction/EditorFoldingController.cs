using System.Windows.Threading;
using CircuitEditor.Core.Code;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
namespace CircuitEditor.App.Interaction;
internal sealed class EditorFoldingController : IDisposable
{
    private const int FoldingDelayMilliseconds=450;
    private readonly TextEditor editor;
    private FoldingManager manager;
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(FoldingDelayMilliseconds)};
    private int revision;
    private bool disposed;
    internal EditorFoldingController(TextEditor editor)
    {this.editor=editor;manager=FoldingManager.Install(editor.TextArea);editor.TextChanged+=Changed;timer.Tick+=Tick;Refresh();}
    internal void Refresh()
    {
        // FoldingManager belongs to a document; a new project requires a fresh manager.
        FoldingManager.Uninstall(manager);manager=FoldingManager.Install(editor.TextArea);Changed(this,EventArgs.Empty);
    }
    private void Changed(object? sender,EventArgs e){revision++;timer.Stop();timer.Start();}
    private async void Tick(object? sender,EventArgs e)
    {
        timer.Stop();var text=editor.Text;var version=revision;
        var ranges=await Task.Run(()=>Ranges(text));
        if(!disposed&&version==revision)manager.UpdateFoldings(ranges.Select(r=>new NewFolding(r.Start,r.End)), -1);
    }
    internal static IReadOnlyList<(int Start,int End)> Ranges(string text)
    {
        var result=new List<(int,int)>();var stack=new Stack<CppToken>();
        foreach(var token in CppTokenScanner.Scan(text))
        {
            if(token.Text=="{")stack.Push(token);
            else if(token.Text=="}"&&stack.TryPop(out var open)&&open.Line<token.Line)result.Add((open.Offset,token.Offset+1));
        }
        return result.OrderBy(r=>r.Item1).ToArray();
    }
    public void Dispose(){disposed=true;revision++;timer.Stop();timer.Tick-=Tick;editor.TextChanged-=Changed;FoldingManager.Uninstall(manager);}
}
