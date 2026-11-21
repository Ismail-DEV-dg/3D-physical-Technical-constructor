namespace CircuitEditor.Core.Scene;

/// <summary>Фильтр отображения трёхмерного представления, независимый от формата проекта.</summary>
public readonly record struct SceneLayerVisibility(bool Wires, bool Labels, bool Boards, bool Breadboard)
{
    /// <summary>Определяет, отображается ли слой. Обычные детали видимы всегда.</summary>
    public bool Shows(SceneLayer layer) => layer switch
    {
        SceneLayer.Wire => Wires,
        SceneLayer.Label => Labels,
        SceneLayer.Board => Boards,
        SceneLayer.Breadboard => Breadboard,
        _ => true
    };
}
