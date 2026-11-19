using CircuitEditor.Core.Code;
namespace CircuitEditor.Core.Repositories;
/// <summary>Читает шаблоны генерации и таблицы плат.</summary>
public interface ICodeTemplateRepository
{
    /// <summary>Загружает проверенный каталог.</summary>
    RepositoryResult<CodeCatalog> Load();
}
