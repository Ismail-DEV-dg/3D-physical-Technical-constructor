using CircuitEditor.Core.Code;
namespace CircuitEditor.Core.Repositories;
/// <summary>Читает словарь Arduino API.</summary>
public interface ICodeCompletionRepository
{
    /// <summary>Загружает проверенные элементы API.</summary>
    RepositoryResult<IReadOnlyList<CodeCompletionItem>> Load();
}
