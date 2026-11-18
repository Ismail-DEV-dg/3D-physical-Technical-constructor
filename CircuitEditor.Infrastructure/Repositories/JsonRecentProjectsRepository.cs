using System.Text.Json;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class JsonRecentProjectsRepository : IRecentProjectsRepository
{
    private const int Limit = 10;
    private readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CircuitEditor", "recent.json");
    public IReadOnlyList<string> Load()
    {
        try { return (JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Take(Limit).ToArray(); }
        catch (Exception e) when (JsonProjectRepository.IsDataError(e)) { return []; }
    }
    public void Add(string file)
    {
        try { AtomicFile.Write(path, JsonSerializer.SerializeToUtf8Bytes(new[] { Path.GetFullPath(file) }.Concat(Load())
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(Limit))); }
        catch (Exception e) when (JsonProjectRepository.IsDataError(e)) { }
    }
}
