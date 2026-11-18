using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class ErrorLogRepository : IErrorLogRepository
{
    public bool TryAppend(string message)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CircuitEditor");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "errors.log"), $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }
}
