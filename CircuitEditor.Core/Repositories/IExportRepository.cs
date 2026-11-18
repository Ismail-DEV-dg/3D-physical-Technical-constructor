namespace CircuitEditor.Core.Repositories;
/// <summary>Записывает сформированные экспортные данные.</summary>
public interface IExportRepository
{
    /// <summary>Сохраняет байты либо возвращает понятную ошибку.</summary>
    string? Write(string path, byte[] data);
}
