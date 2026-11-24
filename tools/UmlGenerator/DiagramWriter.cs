using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UmlGenerator;

internal static class DiagramWriter
{
    private const int TypesPerDiagram = 20;
    private const int MembersPerType = 6;

    public static SortedDictionary<string, string> Build(SourceCatalog catalog)
    {
        SortedDictionary<string, string> files = new(StringComparer.Ordinal);
        var overview = new StringBuilder("flowchart LR\n");
        foreach (var project in catalog.Projects)
        {
            var label = Path.GetFileNameWithoutExtension(project);
            overview.AppendLine($"    {ProjectId(label)}[\"{label}\"]");
            foreach (var reference in catalog.References(project))
                overview.AppendLine($"    {ProjectId(label)} --> {ProjectId(reference)}");
        }
        files.Add("00-projects.mmd", Text(overview));

        var definitions = catalog.ReadTypes();
        string[] model = ["CircuitProject", "Circuit", "Component", "ComponentDefinition", "Pin", "PinReference", "Wire",
            "WireEndpoint", "BreadboardDefinition", "BreadboardInstance", "BreadboardHole", "ContactBinding",
            "FootprintDefinition", "ViewLayout", "ComponentPlacement", "Netlist", "NetNode"];
        files.Add("01-project-model.mmd", Draw(definitions.Where(type => type.Project == "CircuitEditor.Core" && model.Contains(type.Name)).ToArray()));
        var groups = definitions.GroupBy(type => (type.Area, type.Project));
        foreach (var group in groups)
        {
            var prefix = $"{group.Key.Area}-{group.Key.Project.Split('.')[^1].ToLowerInvariant()}";
            var index = 0;
            foreach (var chunk in group.Chunk(TypesPerDiagram))
                files.Add($"{prefix}-{++index:D2}.mmd", Draw(chunk));
        }
        return files;
    }

    private static string Draw(SourceType[] types)
    {
        var text = new StringBuilder("classDiagram\n    direction LR\n");
        var names = types.Select((type, index) => (type, id: $"T{index + 1:D2}"))
            .ToDictionary(row => row.type.QualifiedName, row => row.id);
        foreach (var type in types)
        {
            var id = names[type.QualifiedName];
            text.AppendLine($"    class {id}[\"{type.Name}\"]");
            if (type.IsInterface || type.IsEnumeration)
                text.AppendLine($"    <<{(type.IsInterface ? "interface" : "enumeration")}>> {id}");
            foreach (var member in type.Members.Take(MembersPerType))
                text.AppendLine($"    {id} : {member}");
        }

        foreach (var type in types)
        {
            var source = names[type.QualifiedName];
            var bases = type.BaseTypes.SelectMany(reference => Match(reference, type, types)).Distinct().ToArray();
            foreach (var parent in bases)
                text.AppendLine($"    {source} {(parent.IsInterface && !type.IsInterface ? "..|>" : "--|>")} {names[parent.QualifiedName]}");
            foreach (var target in type.Dependencies.SelectMany(reference => Match(reference, type, types)).Distinct().Except(bases))
                text.AppendLine($"    {source} ..> {names[target.QualifiedName]}");
        }
        return Text(text);
    }

    private static IEnumerable<SourceType> Match(TypeSyntax reference, SourceType source, SourceType[] types)
    {
        foreach (var name in reference.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().Select(part => part.Identifier.ValueText))
        {
            var matches = types.Where(type => type.Name == name && type != source).ToArray();
            if (matches.Length == 1) yield return matches[0];
            else if (matches.FirstOrDefault(type => type.Namespace == source.Namespace) is { } local) yield return local;
        }
    }

    private static string ProjectId(string project) => project.Replace('.', '_');
    private static string Text(StringBuilder text) => text.ToString().ReplaceLineEndings("\n");
}
