namespace CircuitEditor.Core.Repositories;
/// <summary>Экспортирует скетч в отдельную папку Arduino с одноимённым .ino.</summary>
public interface ISketchRepository
{
    /// <summary>Возвращает фактический путь или сообщение об ошибке.</summary>
    RepositoryResult<string> Export(string selectedPath, string text);
}
