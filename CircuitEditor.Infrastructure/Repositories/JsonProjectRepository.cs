using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class JsonProjectRepository(IEnumerable<IProjectMigration> migrations, string? recoveryPath = null) : IProjectRepository
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };
    public string RecoveryPath => recoveryPath ?? Path.Combine(Path.GetTempPath(), "CircuitEditor", "recovery.circ");
    public bool HasRecovery => File.Exists(RecoveryPath);
    public RepositoryResult<ProjectDocument> Load(string path)
    {
        try
        {
            var node = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new ArgumentException("Пустой файл проекта.");
            var version = node["schemaVersion"]?.GetValue<int>() ?? throw new ArgumentException("Нет schemaVersion.");
            if (version > ProjectDocument.CurrentSchemaVersion) throw new ArgumentException($"Версия .circ {version} новее поддерживаемой версии 1.");
            var visited = new HashSet<int>();
            while (version != ProjectDocument.CurrentSchemaVersion)
            {
                if (!visited.Add(version)) throw new ArgumentException("Цикл миграций проекта.");
                var migration = migrations.SingleOrDefault(m => m.FromVersion == version)
                    ?? throw new ArgumentException($"Не поддерживается старая версия .circ: {version}.");
                if (migration.ToVersion <= version || migration.ToVersion > ProjectDocument.CurrentSchemaVersion)
                    throw new ArgumentException("Неверная цепочка миграций.");
                node = migration.Migrate(node.DeepClone().AsObject()); version = migration.ToVersion;
                node["schemaVersion"] = version;
            }
            return new(node.Deserialize<ProjectDocument>(Options) ?? throw new ArgumentException("Пустой проект."), null);
        }
        catch (Exception e) when (IsDataError(e)) { return new(null, "Не удалось открыть проект: " + e.Message); }
    }
    public string? Save(string path, ProjectDocument document)
    {
        try { return AtomicFile.Write(path, JsonSerializer.SerializeToUtf8Bytes(document, Options)); }
        catch (Exception e) when (IsDataError(e)) { return "Не удалось сохранить проект: " + e.Message; }
    }
    public void ClearRecovery() { try { File.Delete(RecoveryPath); } catch (Exception e) when (IsDataError(e)) { } }
    internal static bool IsDataError(Exception e) => e is IOException or UnauthorizedAccessException or JsonException
        or ArgumentException or InvalidOperationException or NotSupportedException or NullReferenceException or OverflowException;
}
