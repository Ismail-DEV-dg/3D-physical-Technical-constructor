using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Core.Repositories;

/// <summary>Источник встроенных сценариев для контроллеров.</summary>
public interface IScenarioRepository
{
    /// <summary>Читает каталог сценариев или возвращает ошибку данных.</summary>
    RepositoryResult<IReadOnlyList<ScenarioTemplate>> Load();
}
