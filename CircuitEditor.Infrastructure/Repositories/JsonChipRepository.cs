using System.Text.Json;
using System.Text.Json.Nodes;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonChipRepository(ILogicFamilyRepository profiles, IEnumerable<IChipMigration> migrations,
    string? directory = null) : IChipRepository
{
    private const long MaximumFileBytes = 2_097_152;
    private readonly object gate = new();
    private string DirectoryPath => directory ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CircuitEditor", "Chips");

    public RepositoryResult<IReadOnlyList<ChipDefinition>> List()
    {
        List<ChipDefinition> loaded = [];
        List<string> warnings = [];
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.chip").Order(StringComparer.Ordinal))
            {
                var result = Load(path);
                if (result.Value is { } chip && Path.GetFileNameWithoutExtension(path) == chip.Id.ToString("D"))
                    loaded.Add(chip);
                else
                    warnings.Add(Path.GetFileName(path) + ": " + (result.Error ?? "Имя не соответствует ID."));
            }
            return new(loaded.OrderBy(chip => chip.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(), null)
                { Warning = warnings.Count == 0 ? null : string.Join(Environment.NewLine, warnings) };
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, "Не удалось прочитать библиотеку микросхем: " + error.Message);
        }
    }

    public RepositoryResult<ChipDefinition> Load(string path)
    {
        try
        {
            if (new FileInfo(path).Length > MaximumFileBytes) throw new ArgumentException("Файл .chip превышает 2 МБ.");
            var data = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new ArgumentException("Пустой файл .chip.");
            var version = data["schemaVersion"]?.GetValue<int>() ?? throw new ArgumentException("Нет schemaVersion.");
            while (version < ChipFileDocument.CurrentSchemaVersion)
            {
                var conversion = migrations.SingleOrDefault(item => item.FromVersion == version);
                if (conversion is null || conversion.ToVersion <= version || conversion.ToVersion > ChipFileDocument.CurrentSchemaVersion)
                    throw new ArgumentException(VersionMessage(version));
                data = conversion.Migrate(data);
                version = conversion.ToVersion;
                data["schemaVersion"] = version;
            }
            if (version != ChipFileDocument.CurrentSchemaVersion) throw new ArgumentException(VersionMessage(version));
            var chip = data.Deserialize<ChipFileDocument>(JsonProjectRepository.Options)?.Definition ??
                throw new ArgumentException("Файл не содержит определения микросхемы.");
            return Validate(chip) is { } problem ? new(null, problem) : new(chip, null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, "Не удалось загрузить .chip: " + error.Message);
        }
    }

    public string? Export(string path, ChipDefinition definition)
    {
        try
        {
            if (Validate(definition) is { } problem) return problem;
            var data = JsonSerializer.SerializeToUtf8Bytes(new ChipFileDocument(ChipFileDocument.CurrentSchemaVersion, definition),
                JsonProjectRepository.Options);
            if (data.Length > MaximumFileBytes) return "Файл .chip превышает 2 МБ.";
            return AtomicFile.Write(path, data);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return "Не удалось сохранить .chip: " + error.Message;
        }
    }

    public string? Store(ChipDefinition definition, int? expectedVersion = null)
    {
        lock (gate)
        {
            var path = LibraryFile(definition.Id);
            if (CheckRevision(path, expectedVersion) is { } problem) return problem;
            if (expectedVersion.HasValue && definition.DefinitionVersion <= expectedVersion)
                return "Новая ревизия должна быть больше текущей.";
            return Export(path, definition);
        }
    }

    public string? Delete(Guid id, int expectedVersion)
    {
        lock (gate)
        {
            try
            {
                var path = LibraryFile(id);
                if (CheckRevision(path, expectedVersion) is { } problem) return problem;
                File.Delete(path);
                return null;
            }
            catch (Exception error) when (JsonProjectRepository.IsDataError(error))
            {
                return "Не удалось удалить микросхему: " + error.Message;
            }
        }
    }

    private string? CheckRevision(string path, int? expected)
    {
        if (!File.Exists(path)) return expected is null ? null : "Ревизия микросхемы уже удалена.";
        if (expected is null) return "Микросхема с таким ID уже есть в библиотеке.";
        var actual = Load(path);
        if (actual.Value is null) return actual.Error;
        return actual.Value.DefinitionVersion == expected ? null : "Ревизия в библиотеке изменилась. Загрузите её снова.";
    }

    private string LibraryFile(Guid id) => Path.Combine(DirectoryPath, id.ToString("D") + ".chip");

    private string? Validate(ChipDefinition definition)
    {
        var families = profiles.Load();
        if (families.Value is null) return families.Error ?? "Нет профилей логических семейств.";
        var errors = ChipValidator.Validate(definition, families.Value);
        return errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);
    }

    private static string VersionMessage(int version) =>
        $"Версия .chip: {version}. Поддерживаемая версия: {ChipFileDocument.CurrentSchemaVersion}.";
}
