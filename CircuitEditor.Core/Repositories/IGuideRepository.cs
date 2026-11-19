using CircuitEditor.Core.Diagnostics;

namespace CircuitEditor.Core.Repositories;

/// <summary>Источник локальных пособий по платам, деталям и приложению.</summary>
public interface IGuideRepository
{
    /// <summary>Читает и проверяет комплект пособий.</summary>
    RepositoryResult<GuideLibrary> Load();
}
