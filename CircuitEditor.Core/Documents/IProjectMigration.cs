using System.Text.Json.Nodes;
namespace CircuitEditor.Core.Documents;
/// <summary>Шаг преобразования сохранённого проекта к следующей версии формата.</summary>
public interface IProjectMigration
{
    /// <summary>Исходная версия.</summary>
    int FromVersion { get; }
    /// <summary>Целевая версия.</summary>
    int ToVersion { get; }
    /// <summary>Преобразует независимый JSON-документ.</summary>
    JsonObject Migrate(JsonObject document);
}
