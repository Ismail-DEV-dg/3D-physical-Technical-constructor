using CircuitEditor.Core.Chips;

namespace CircuitEditor.Core.Repositories;

/// <summary>Доступ к пользовательской библиотеке и отдельным файлам .chip.</summary>
public interface IChipRepository
{
    /// <summary>Читает актуальные версии доступных пользовательских микросхем.</summary>
    RepositoryResult<IReadOnlyList<ChipDefinition>> List();
    /// <summary>Читает один файл, применяет миграции и проверяет определение.</summary>
    RepositoryResult<ChipDefinition> Load(string path);
    /// <summary>Сохраняет отдельный файл для переноса.</summary>
    string? Export(string path, ChipDefinition definition);
    /// <summary>Сохраняет ревизию, ожидая указанную текущую версию или отсутствие файла.</summary>
    string? Store(ChipDefinition definition, int? expectedVersion = null);
    /// <summary>Удаляет указанную версию из пользовательской библиотеки.</summary>
    string? Delete(Guid id, int expectedVersion);
}
