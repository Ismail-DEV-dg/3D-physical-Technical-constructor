using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Scene;

/// <summary>Преобразует снимок проекта в сцену без зависимости от графической платформы.</summary>
public interface ISceneBuilder
{
    /// <summary>Строит сцену из выделенного снимка модели с поддержкой отмены.</summary>
    SceneModel Build(CircuitProject project, CancellationToken cancellationToken = default);
}
