using CircuitEditor.Core.Diagnostics;
namespace CircuitEditor.Core.Repositories;
/// <summary>Читает встроенные статьи об ошибках и способах их исправления.</summary>
public interface IErrorHelpRepository
{
    /// <summary>Возвращает проверенный каталог справки.</summary>
    RepositoryResult<IReadOnlyList<DiagnosticArticle>> Load();
}
