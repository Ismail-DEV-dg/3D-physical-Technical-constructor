using System.Globalization;
using System.Text;

namespace BrandingGenerator;

internal static class BrandSvg
{
    public static string Square(Mark mark) => Wrap(mark.Size, mark.Size, MarkBody(mark));

    public static string Horizontal(Mark mark)
    {
        var inner = $"""
            <rect width="640" height="147" rx="26" fill="#0A0E17"/>
            <g transform="translate(16 16) scale(0.9)">{MarkBody(mark)}</g>
            <text x="153" y="88" font-family="Segoe UI, sans-serif" font-size="48" font-weight="700" fill="#EAF2FF">CIRCUIT <tspan fill="#00E5FF">/</tspan> EDITOR</text>
            """;
        return Wrap(640, 147, inner);
    }

    private static string MarkBody(Mark mark)
    {
        var body = new StringBuilder();
        body.AppendLine(CultureInfo.InvariantCulture, $"<rect x=\"0.7\" y=\"0.7\" width=\"{mark.Size - 1.4}\" height=\"{mark.Size - 1.4}\" rx=\"{mark.CornerRadius}\" fill=\"#0A0E17\" stroke=\"#334260\" stroke-width=\"{(mark.Size <= 32 ? 1.15 : 1.8)}\"/>");
        foreach (var trace in mark.Traces)
            body.AppendLine(CultureInfo.InvariantCulture, $"<path d=\"{trace.Data}\" stroke=\"{trace.Color}\" stroke-width=\"{trace.Width}\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
        foreach (var node in mark.Nodes)
            body.AppendLine(CultureInfo.InvariantCulture, $"<circle cx=\"{node.X}\" cy=\"{node.Y}\" r=\"{node.Radius}\" fill=\"{node.Color}\"/>");
        return body.ToString();
    }

    private static string Wrap(int width, int height, string body) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" width=\"{width}\" height=\"{height}\" role=\"img\" aria-label=\"Circuit Editor\">\n{body}\n</svg>\n".ReplaceLineEndings("\n");
}
