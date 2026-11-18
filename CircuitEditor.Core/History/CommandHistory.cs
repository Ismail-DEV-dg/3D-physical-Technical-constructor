namespace CircuitEditor.Core.History;
/// <summary>Ограниченная история с идентичностью сохранённого состояния.</summary>
public sealed class CommandHistory
{
    /// <summary>Лимит истории по требованиям этапа.</summary>
    public const int Capacity = 200;
    private readonly List<IEditCommand> commands = [];
    private readonly List<Guid> states = [Guid.NewGuid()];
    private int position;
    private Guid saved;
    /// <summary>Создаёт чистую историю.</summary>
    public CommandHistory() => saved = states[0];
    /// <summary>Есть действие для отмены.</summary>
    public bool CanUndo => position > 0;
    /// <summary>Есть действие для повтора.</summary>
    public bool CanRedo => position < commands.Count;
    /// <summary>Текущее состояние отличается от сохранённого.</summary>
    public bool IsDirty => states[position] != saved;
    /// <summary>Количество доступных действий.</summary>
    public int Count => commands.Count;
    /// <summary>Уведомление для команд интерфейса.</summary>
    public event EventHandler? Changed;
    /// <summary>Выполняет новую команду.</summary>
    public void Execute(IEditCommand command) { command.Execute(); Record(command); }
    /// <summary>Записывает уже выполненный целый жест.</summary>
    public void Record(IEditCommand command)
    {
        commands.RemoveRange(position, commands.Count - position);
        states.RemoveRange(position + 1, states.Count - position - 1);
        commands.Add(command); states.Add(Guid.NewGuid()); position++;
        if (commands.Count > Capacity) { commands.RemoveAt(0); states.RemoveAt(0); position--; }
        Changed?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Отменяет одно действие.</summary>
    public void Undo() { if (!CanUndo) return; commands[--position].Undo(); Changed?.Invoke(this, EventArgs.Empty); }
    /// <summary>Повторяет одно действие.</summary>
    public void Redo() { if (!CanRedo) return; commands[position++].Execute(); Changed?.Invoke(this, EventArgs.Empty); }
    /// <summary>Отмечает текущее состояние сохранённым.</summary>
    public void MarkSaved() { saved = states[position]; Changed?.Invoke(this, EventArgs.Empty); }
    /// <summary>Помечает восстановленный после сбоя документ несохранённым.</summary>
    public void MarkUnsaved() { saved = Guid.Empty; Changed?.Invoke(this, EventArgs.Empty); }
    /// <summary>Очищает историю при создании/открытии.</summary>
    public void Clear() { commands.Clear(); states.Clear(); states.Add(Guid.NewGuid()); position = 0; MarkSaved(); }
}
