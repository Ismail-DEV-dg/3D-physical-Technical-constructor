using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Models;

/// <summary>Сериализуемая модель соединения; интерактивные перемычки будут на этапе 4.</summary>
public sealed class Wire
{
    /// <summary>Создаёт соединение с новым ID.</summary>
    public Wire(PinReference start, PinReference end) : this(Guid.NewGuid(), start, end) { }
    /// <summary>Восстанавливает соединение с прежним ID.</summary>
    [JsonConstructor]
    public Wire(Guid id, PinReference start, PinReference end)
    {
        ArgumentNullException.ThrowIfNull(start); ArgumentNullException.ThrowIfNull(end);
        if (id == Guid.Empty || start.ComponentId == Guid.Empty || end.ComponentId == Guid.Empty
            || string.IsNullOrWhiteSpace(start.PinId) || string.IsNullOrWhiteSpace(end.PinId) || start == end)
            throw new ArgumentException("Некорректные концы провода.");
        Id = id; Start = start; End = end;
    }
    /// <summary>Идентификатор.</summary>
    public Guid Id { get; }
    /// <summary>Первый вывод.</summary>
    public PinReference Start { get; }
    /// <summary>Второй вывод.</summary>
    public PinReference End { get; }
}
