using System.Text;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class FileSketchRepository : ISketchRepository
{
    public RepositoryResult<string> Export(string selectedPath, string text)
    {
        try
        {
            var path = Path.GetFullPath(selectedPath);
            var name = Path.GetFileNameWithoutExtension(path);
            if (!SketchName.IsValid(name))
                return new(null, "Имя скетча: латинские буквы, цифры, _, точка и дефис; начало — буква, цифра или _. До 63 символов, без точки в конце и зарезервированных имён.");
            var parent = Path.GetDirectoryName(path)!;
            var folder = Path.GetFileName(parent).Equals(name, StringComparison.OrdinalIgnoreCase) ? parent : Path.Combine(parent, name);
            var target = Path.Combine(folder, name + ".ino");
            var error = AtomicFile.Write(target, new UTF8Encoding(false).GetBytes(text));
            return error is null ? new(target, null) : new(null, error);
        }
        catch (Exception e) when (JsonProjectRepository.IsDataError(e)) { return new(null, "Не удалось экспортировать скетч: " + e.Message); }
    }
}
