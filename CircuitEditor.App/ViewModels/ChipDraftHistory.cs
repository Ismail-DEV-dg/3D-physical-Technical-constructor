namespace CircuitEditor.App.ViewModels;

internal sealed class ChipDraftHistory<T> where T : class
{
    private const int Capacity = 200;
    private readonly LinkedList<T> undo = [];
    private readonly Stack<T> redo = new();

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public void Record(T previous)
    {
        undo.AddLast(previous);
        if (undo.Count > Capacity) undo.RemoveFirst();
        redo.Clear();
    }

    public T Undo(T current)
    {
        if (undo.Last is not { } item) return current;
        undo.RemoveLast();
        redo.Push(current);
        return item.Value;
    }

    public T Redo(T current)
    {
        if (!redo.TryPop(out var value)) return current;
        undo.AddLast(current);
        return value;
    }

    public void Clear() { undo.Clear(); redo.Clear(); }
}
