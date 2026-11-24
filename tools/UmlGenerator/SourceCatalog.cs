using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UmlGenerator;

internal sealed class SourceCatalog(string root)
{
    private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase) { "obj", "bin", ".vs", "TestResults" };

    public string[] Projects { get; } = Directory.GetDirectories(root, "CircuitEditor.*")
        .Select(directory => Path.Combine(directory, Path.GetFileName(directory) + ".csproj"))
        .Where(File.Exists).Order(StringComparer.Ordinal).ToArray();

    public IEnumerable<string> References(string project) => XDocument.Load(project).Descendants("ProjectReference")
        .Select(element => Path.GetFileNameWithoutExtension(((string?)element.Attribute("Include") ?? "").Replace('\\', '/')))
        .Order(StringComparer.Ordinal);

    public SourceType[] ReadTypes()
    {
        return Projects.SelectMany(project =>
        {
            var name = Path.GetFileNameWithoutExtension(project);
            var directory = Path.GetDirectoryName(project)!;
            return Files(directory).Where(path => path.EndsWith(".cs", StringComparison.Ordinal))
                .SelectMany(path => ReadFile(path, name, Area(name, Path.GetRelativePath(directory, path))))
                .ToArray();
        }).GroupBy(type => (type.Project, type.QualifiedName)).Select(parts => parts.First() with
        {
            Declarations = parts.SelectMany(type => type.Declarations).ToArray()
        }).OrderBy(type => type.QualifiedName, StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> Files(string directory) => Directory.EnumerateFiles(directory, "*.cs")
        .Concat(Directory.EnumerateDirectories(directory).Where(path => !Ignored.Contains(Path.GetFileName(path)))
            .Order(StringComparer.Ordinal).SelectMany(Files))
        .Order(StringComparer.Ordinal);

    private static IEnumerable<SourceType> ReadFile(string file, string project, string area)
    {
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file).GetCompilationUnitRoot();
        return root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .Where(type => type.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax)
            .Select(type => new SourceType(project, area, string.Join(".", type.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(space => space.Name)),
                type.Identifier.ValueText, [type]));
    }

    private static string Area(string project, string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar);
        if (project.EndsWith(".Tests", StringComparison.Ordinal)) return "tests";
        if (segments.Contains("ThreeD") || segments.Contains("Scene")) return "3d";
        if (segments.Contains("Chips") || segments[0].StartsWith("Chip", StringComparison.Ordinal)) return "chips";
        if (project.EndsWith(".Simulation", StringComparison.Ordinal) || segments[0] == "Simulation") return "simulation";
        return segments[0] switch
        {
            "Models" or "Physical" or "Placement" or "Breadboards" or "History" or "Layout" or "Wiring" => "model",
            "Netlist" or "Validation" => "netlist-erc",
            "Electronics" => "calculations",
            "Code" => "code",
            "Repositories" or "Documents" or "Data" or "Library" => "repositories-formats",
            "ViewModels" or "Views" or "Controls" or "Resources" or "Interaction" => "views-viewmodels",
            _ => "support"
        };
    }
}
