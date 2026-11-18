using CircuitEditor.Core.Physical;

namespace CircuitEditor.Core.Repositories;

/// <summary>Источник физических корпусов компонентов.</summary>
public interface IFootprintRepository
{
    /// <summary>Читает и проверяет библиотеку геометрии.</summary>
    RepositoryResult<IReadOnlyList<FootprintDefinition>> Load();
}
