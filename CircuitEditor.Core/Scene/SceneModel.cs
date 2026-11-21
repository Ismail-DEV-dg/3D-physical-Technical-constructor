using System.Collections.Immutable;

namespace CircuitEditor.Core.Scene;

/// <summary>Неизменяемый снимок 3D-геометрии одного зафиксированного «Макета».</summary>
public sealed record SceneModel(ImmutableArray<SceneNode> Nodes, SceneBounds Bounds)
{
    /// <summary>Пустой вид до загрузки проекта.</summary>
    public static SceneModel Empty { get; } = new([], default);
}
