namespace BrandingGenerator;

internal sealed record Mark(string Name, int Size, double CornerRadius, IReadOnlyList<Trace> Traces, IReadOnlyList<Node> Nodes);
