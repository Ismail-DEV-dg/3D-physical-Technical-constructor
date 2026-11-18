using CircuitEditor.Core.Documents;
namespace CircuitEditor.Core.Repositories;
/// <summary>Единственная точка чтения и записи проектов и автосохранений.</summary>
public interface IProjectRepository
{
    /// <summary>Читает и мигрирует независимый документ.</summary>
    RepositoryResult<ProjectDocument> Load(string path);
    /// <summary>Атомарно сохраняет документ, возвращает ошибку либо null.</summary>
    string? Save(string path, ProjectDocument document);
    /// <summary>Путь восстановления во временной папке.</summary>
    string RecoveryPath { get; }
    /// <summary>Проверяет наличие восстановления.</summary>
    bool HasRecovery { get; }
    /// <summary>Удаляет восстановление после нормального завершения.</summary>
    void ClearRecovery();
}
