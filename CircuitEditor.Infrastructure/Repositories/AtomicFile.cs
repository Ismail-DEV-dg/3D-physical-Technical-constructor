namespace CircuitEditor.Infrastructure.Repositories;
internal static class AtomicFile
{
    internal static string? Write(string path, byte[] data)
    {
        string? temporary = null;
        try
        {
            var full = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(data); stream.Flush(true); }
            File.Move(temporary, full, true); return null;
        }
        catch (Exception e) when (JsonProjectRepository.IsDataError(e)) { return "Не удалось записать файл: " + e.Message; }
        finally { if (temporary is not null) { try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { } } }
    }
}
