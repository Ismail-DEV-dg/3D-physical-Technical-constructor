using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class FileExportRepository : IExportRepository
{
    public string? Write(string path, byte[] data) => AtomicFile.Write(path, data);
}
