using System.Text.Json;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonProjectSampleRepository : IProjectSampleRepository
{
    private readonly string directory = Path.Combine(AppContext.BaseDirectory, "Samples");

    public IReadOnlyList<ProjectSample> Load()
    {
        try
        {
            using var stream = File.OpenRead(Path.Combine(directory, "index.json"));
            return JsonSerializer.Deserialize<ProjectSample[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    public string PathFor(ProjectSample sample) => Path.Combine(directory, Path.GetFileName(sample.File));

    public bool IsOriginal(string path) =>
        string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), directory, StringComparison.OrdinalIgnoreCase);
}
