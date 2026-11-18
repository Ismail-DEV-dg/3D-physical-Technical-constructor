namespace CircuitEditor.Core.History;
/// <summary>Обратимое изменение модели без UI.</summary>
public interface IEditCommand
{
    /// <summary>Название действия.</summary>
    string Name { get; }
    /// <summary>Применяет действие.</summary>
    void Execute();
    /// <summary>Отменяет действие.</summary>
    void Undo();
}
