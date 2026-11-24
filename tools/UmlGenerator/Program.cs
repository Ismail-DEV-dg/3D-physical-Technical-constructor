using System.Text;
using UmlGenerator;

var root = Path.GetFullPath(args.FirstOrDefault(arg => arg != "--check") ?? ".");
if (!File.Exists(Path.Combine(root, "CircuitEditor.sln")))
{
    Console.Error.WriteLine("Укажите корень решения CircuitEditor.sln.");
    return 2;
}

var check = args.Contains("--check", StringComparer.Ordinal);
var directory = Path.Combine(root, "docs", "uml", "generated");
var diagrams = DiagramWriter.Build(new SourceCatalog(root));
if (!check) Directory.CreateDirectory(directory);
var stale = false;
var existing = Directory.Exists(directory) ? Directory.GetFiles(directory, "*.mmd") : [];
foreach (var file in existing.Where(file => !diagrams.ContainsKey(Path.GetFileName(file))))
{
    if (check) { Console.Error.WriteLine($"Лишняя диаграмма: {Path.GetFileName(file)}"); stale = true; }
    else File.Delete(file);
}

foreach (var (name, diagram) in diagrams)
{
    var destination = Path.Combine(directory, name);
    if (!check)
        File.WriteAllText(destination, diagram, new UTF8Encoding(false));
    else if (!File.Exists(destination) || File.ReadAllText(destination).ReplaceLineEndings("\n") != diagram)
    {
        Console.Error.WriteLine($"Диаграмма требует обновления: {name}");
        stale = true;
    }
}

Console.WriteLine($"Диаграмм: {diagrams.Count}.");
return stale ? 1 : 0;
