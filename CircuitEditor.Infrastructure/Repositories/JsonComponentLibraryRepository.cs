using CircuitEditor.Core.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Infrastructure.Repositories;

/// <summary>Читает встроенные JSON-файлы библиотеки и проверяет уникальность записей.</summary>
public sealed class JsonComponentLibraryRepository : IComponentLibraryRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter<PinType>(allowIntegerValues: false) }
    };

    /// <inheritdoc/>
    public LibraryLoadResult Load()
    {
        var definitions = new List<ComponentDefinition>();
        var assembly = typeof(JsonComponentLibraryRepository).Assembly;
        try
        {
            var names = assembly.GetManifestResourceNames()
                .Where(name => name.Contains(".Library.", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal).ToArray();
            if (names.Length == 0) return Failure("JSON-библиотека не найдена.");
            foreach (var name in names)
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                definitions.AddRange(ReadDefinitions(stream));
            }
            return Validate(definitions);
        }
        catch (Exception exception) when (exception is IOException or JsonException or ArgumentException
                                         or InvalidOperationException or NotSupportedException)
        {
            return Failure($"Не удалось загрузить библиотеку: {exception.Message}");
        }
    }

    /// <summary>Проверяет внешний JSON-поток; ошибка не оставляет частично загруженную библиотеку.</summary>
    public static LibraryLoadResult Read(Stream stream)
    {
        try { return Validate(ReadDefinitions(stream)); }
        catch (Exception exception) when (exception is IOException or JsonException or ArgumentException
                                         or InvalidOperationException or NotSupportedException)
        {
            return Failure($"Некорректная библиотека: {exception.Message}");
        }
    }

    private static List<ComponentDefinition> ReadDefinitions(Stream stream) =>
        JsonSerializer.Deserialize<List<ComponentDefinition>>(stream, Options)
        ?? throw new JsonException("Ожидался массив компонентов.");

    private static LibraryLoadResult Validate(List<ComponentDefinition> components)
    {
        if (components.Count == 0 || components.Any(item => item is null)
            || components.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != components.Count)
            return Failure("Пустая библиотека или повторяющиеся идентификаторы.");
        return new LibraryLoadResult(components.AsReadOnly(), null);
    }

    private static LibraryLoadResult Failure(string error) => new(Array.Empty<ComponentDefinition>(), error);
}
