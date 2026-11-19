using System.Text.Json.Nodes;

namespace CircuitEditor.Core.Repositories;

/// <summary>Преобразование одного старого формата .chip в следующий.</summary>
public interface IChipMigration
{
    /// <summary>Версия исходного контейнера.</summary>
    int FromVersion { get; }
    /// <summary>Версия преобразованного контейнера.</summary>
    int ToVersion { get; }
    /// <summary>Преобразует документ внутри цепочки миграций.</summary>
    JsonObject Migrate(JsonObject source);
}
