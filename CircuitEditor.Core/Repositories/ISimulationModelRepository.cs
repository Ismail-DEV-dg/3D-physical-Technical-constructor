using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Core.Repositories;

/// <summary>Загрузка электрических параметров симуляции.</summary>
public interface ISimulationModelRepository
{
    /// <summary>Возвращает проверенную библиотеку моделей.</summary>
    RepositoryResult<SimulationModelCatalog> Load();
}
