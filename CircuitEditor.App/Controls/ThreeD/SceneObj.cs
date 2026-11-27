using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using CircuitEditor.Core.Repositories;
using HelixToolkit.Wpf;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneObj
{
    private const int TextureSize = 256;

    public static string? Write(Viewport3D viewport, string path, IExportRepository repository)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        var name = Path.GetFileNameWithoutExtension(fullPath);
        var materialName = name + ".mtl";
        if (string.IsNullOrWhiteSpace(name)) return "Укажите имя файла OBJ.";
        using MemoryStream content = new();
        Dictionary<string, MemoryStream> sidecars = new(StringComparer.OrdinalIgnoreCase);
        var baseDirectory = Path.EndsInDirectorySeparator(directory) ? directory : directory + Path.DirectorySeparatorChar;
        ObjExporter exporter = new()
        {
            MaterialsFile = materialName,
            TextureFolder = name + "-textures",
            TextureSize = TextureSize,
            ExportNormals = true,
            SwitchYZ = false,
            UseDissolveForTransparency = true,
            Comment = "Circuit Editor, millimeters, Z up",
            FileCreator = relative =>
            {
                var sidecar = Path.GetFullPath(relative, baseDirectory);
                if (!sidecar.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sidecar, fullPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Некорректное имя материала или текстуры.");
                if (sidecars.Remove(sidecar, out var old)) old.Dispose();
                return sidecars[sidecar] = new MemoryStream();
            }
        };
        try
        {
            exporter.Export(viewport, content);
            var materialPath = Path.Combine(directory, materialName);
            foreach (var (file, stream) in sidecars)
            {
                if (string.Equals(file, materialPath, StringComparison.OrdinalIgnoreCase)) continue;
                if (repository.Write(file, stream.ToArray()) is { } error) return error;
            }
            if (!sidecars.TryGetValue(materialPath, out var materials)) return "Экспортёр не создал файл материалов.";
            var textures = sidecars.Keys.Where(file => !string.Equals(file, materialPath, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(file => Path.GetFileName(file), file => Path.GetRelativePath(directory, file).Replace('\\', '/'),
                    StringComparer.OrdinalIgnoreCase);
            var materialBytes = ResolveTexturePaths(materials.ToArray(), textures);
            if (repository.Write(materialPath, materialBytes) is { } materialError) return materialError;
            return repository.Write(fullPath, content.ToArray());
        }
        finally
        {
            foreach (var stream in sidecars.Values) stream.Dispose();
        }
    }

    private static byte[] ResolveTexturePaths(byte[] bytes, IReadOnlyDictionary<string, string> textures)
    {
        if (textures.Count == 0) return bytes;
        // Helix renders texture files in TextureFolder but uses only their names in map_Kd.
        var text = Regex.Replace(Encoding.UTF8.GetString(bytes), @"(?m)^(map_Kd[ \t]+)([^\r\n]+)", match =>
            textures.TryGetValue(match.Groups[2].Value.Trim(), out var path) ? match.Groups[1].Value + path : match.Value);
        return Encoding.UTF8.GetBytes(text);
    }
}
