using System.Text.Json;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonShortcutSettingsRepository : IShortcutSettingsRepository
{
    private readonly string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CircuitEditor", "shortcuts.json");

    public IReadOnlyDictionary<string, string> Load()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []; }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error)) { return new Dictionary<string, string>(); }
    }

    public string? Save(IReadOnlyDictionary<string, string> overrides)
    {
        try
        {
            AtomicFile.Write(path, JsonSerializer.SerializeToUtf8Bytes(overrides));
            return null;
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return "Не удалось сохранить сочетания клавиш: " + error.Message;
        }
    }
}
