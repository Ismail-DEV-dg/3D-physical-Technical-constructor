namespace CircuitEditor.Infrastructure.Repositories;

using CircuitEditor.Core.Repositories;

public sealed class FileInstallationLicenseRepository : IInstallationLicenseRepository
{
    public RepositoryResult<string> Read()
    {
        try
        {
            return new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LICENSE")), null);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return new(null, "Не удалось открыть LICENSE. " + FileFailure.Describe(error));
        }
    }
}
