using CircuitEditor.Core.Scene;

namespace CircuitEditor.Core.Repositories;

/// <summary>Загрузка высот, форм и материалов для процедурной сцены.</summary>
public interface ISceneCatalogRepository
{
    /// <summary>Возвращает проверенные профили трёхмерной геометрии.</summary>
    RepositoryResult<SceneCatalog> Load();
}
