namespace CircuitEditor.Core.Scene;

/// <summary>Сопоставляет визуальные объекты с идентификаторами в проекте.</summary>
public sealed class ScenePickIndex<TVisual> where TVisual : class
{
    private readonly Dictionary<TVisual, Guid> owners = new(ReferenceEqualityComparer.Instance);

    /// <summary>Регистрирует выбираемое представление узла.</summary>
    public void Set(TVisual visual, Guid id) => owners[visual] = id;

    /// <summary>Убирает сопоставление при удалении визуального объекта.</summary>
    public void Remove(TVisual visual) => owners.Remove(visual);

    /// <summary>Возвращает владельца попадания, если у визуального объекта он есть.</summary>
    public Guid? Find(TVisual visual) => owners.TryGetValue(visual, out var result) ? result : null;
}
