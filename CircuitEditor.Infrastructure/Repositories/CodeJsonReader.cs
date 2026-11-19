using System.Text.Json;
namespace CircuitEditor.Infrastructure.Repositories;
internal static class CodeJsonReader
{
    internal static T Read<T>(string file)
    {
        using var stream=typeof(CodeJsonReader).Assembly.GetManifestResourceStream("CircuitEditor.Infrastructure.Data."+file)
            ?? throw new ArgumentException("Нет встроенного каталога "+file);
        return JsonSerializer.Deserialize<T>(stream,JsonProjectRepository.Options) ?? throw new ArgumentException("Пустой каталог кода.");
    }
}
