namespace CircuitEditor.Core.Repositories;

/// <summary>Чтение внешней таблицы истинности.</summary>
public interface IChipCsvRepository
{
    /// <summary>Загружает текст выбранного CSV либо возвращает сообщение об ошибке.</summary>
    RepositoryResult<string> Load(string path);
}
