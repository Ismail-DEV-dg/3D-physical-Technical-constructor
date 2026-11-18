using System.Text.Json.Nodes;
namespace CircuitEditor.Core.Documents;
/// <summary>Расширение механизма миграций; производственных старых версий пока нет.</summary>
public interface IProjectMigration
{
    /// <summary>Исходная версия.</summary>
    int FromVersion { get; }
    /// <summary>Целевая версия.</summary>
    int ToVersion { get; }
    /// <summary>Преобразует независимый JSON-документ.</summary>
    JsonObject Migrate(JsonObject document);
}
