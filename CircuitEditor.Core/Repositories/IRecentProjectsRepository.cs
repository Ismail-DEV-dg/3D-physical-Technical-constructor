namespace CircuitEditor.Core.Repositories;
/// <summary>Хранилище последних открытых файлов.</summary>
public interface IRecentProjectsRepository
{
    /// <summary>Читает список.</summary>
    IReadOnlyList<string> Load();
    /// <summary>Обновляет список последним успешным файлом.</summary>
    void Add(string path);
}
