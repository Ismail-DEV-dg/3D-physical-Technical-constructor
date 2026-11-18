using CircuitEditor.Core.Breadboards;

namespace CircuitEditor.Core.Repositories;

/// <summary>Источник топологии breadboard.</summary>
public interface IBreadboardRepository
{
    /// <summary>Читает и проверяет описание выбранной модели платы.</summary>
    RepositoryResult<BreadboardDefinition> Load();
}
