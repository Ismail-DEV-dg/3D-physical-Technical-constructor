using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class FileChipCsvRepository : IChipCsvRepository
{
    private const long MaximumBytes = 1_048_576;

    public RepositoryResult<string> Load(string path)
    {
        try
        {
            if (new FileInfo(path).Length > MaximumBytes) return new(null, "Файл CSV превышает 1 МБ.");
            return new(File.ReadAllText(path), null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, error.Message);
        }
    }
}
