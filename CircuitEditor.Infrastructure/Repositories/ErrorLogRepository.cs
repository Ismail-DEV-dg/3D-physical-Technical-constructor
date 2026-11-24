using System.Diagnostics;
using System.Reflection;
using System.Text;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class ErrorLogRepository(string? directory = null, long maximumBytes = 256 * 1024) : IErrorLogRepository
{
    private readonly object gate = new();
    private readonly string folder = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CircuitEditor", "logs");

    public bool TryAppend(Exception error)
    {
        try
        {
            var methods = new StackTrace(error, fNeedFileInfo: false).GetFrames()
                .Select(frame => frame.GetMethod())
                .OfType<MethodBase>()
                .Where(method => method.DeclaringType?.Assembly.GetName().Name?.StartsWith("CircuitEditor.", StringComparison.Ordinal) == true)
                .Take(12)
                .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}");
            var entry = $"{DateTimeOffset.UtcNow:O} {error.GetType().FullName} 0x{error.HResult:X8} {string.Join(" ← ", methods)}{Environment.NewLine}";
            lock (gate)
            {
                Directory.CreateDirectory(folder);
                var file = Path.Combine(folder, "errors.log");
                if (File.Exists(file) && new FileInfo(file).Length + Encoding.UTF8.GetByteCount(entry) > maximumBytes)
                    File.Move(file, file + ".1", overwrite: true);
                File.AppendAllText(file, entry);
            }
            return true;
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException) { return false; }
    }
}
