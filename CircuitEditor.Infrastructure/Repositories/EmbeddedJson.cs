using System.Text.Json;
using System.Text.Json.Serialization;

namespace CircuitEditor.Infrastructure.Repositories;

internal static class EmbeddedJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    public static Stream Open(string file) => typeof(EmbeddedJson).Assembly.GetManifestResourceStream(
        $"CircuitEditor.Infrastructure.Data.{file}") ?? throw new IOException($"Нет ресурса {file}.");
    public static T Read<T>(Stream stream) =>
        JsonSerializer.Deserialize<T>(stream, Options) ?? throw new JsonException("Пустое описание.");
    public static bool IsDataError(Exception error) => error is IOException or JsonException or ArgumentException
        or InvalidOperationException or NotSupportedException or OverflowException;
}
