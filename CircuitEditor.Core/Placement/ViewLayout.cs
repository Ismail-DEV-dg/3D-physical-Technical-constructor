using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Placement;

/// <summary>Сериализуемая карта положений экземпляров в одном представлении.</summary>
public sealed class ViewLayout
{
    private readonly Dictionary<Guid, ComponentPlacement> placements;
    /// <summary>Восстанавливает карту, не разделяя изменяемые положения с другой картой.</summary>
    [JsonConstructor]
    public ViewLayout(RepresentationKind kind, IReadOnlyDictionary<Guid, ComponentPlacement>? placements = null)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Неизвестное представление.");
        Kind = kind;
        this.placements = placements?.ToDictionary(pair => pair.Key, pair => pair.Value.Copy()) ?? [];
    }
    /// <summary>Тип представления.</summary>
    public RepresentationKind Kind { get; }
    /// <summary>Размещения экземпляров.</summary>
    public IReadOnlyDictionary<Guid, ComponentPlacement> Placements => placements;
    /// <summary>Возвращает положение зарегистрированного компонента.</summary>
    public ComponentPlacement Get(Guid id) => placements[id];
    /// <summary>Добавляет или заменяет положение независимой копией.</summary>
    public void Set(Guid id, ComponentPlacement placement) => placements[id] = placement.Copy();
    /// <summary>Удаляет положение.</summary>
    public void Remove(Guid id) => placements.Remove(id);
}
