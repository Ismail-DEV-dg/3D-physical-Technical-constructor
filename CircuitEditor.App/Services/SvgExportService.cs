using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using CircuitEditor.App.Controls;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.App.Services;
public sealed class SvgExportService(BreadboardViewModel mockup, SchematicViewModel schematic, IExportRepository repository) : ISvgExportService
{
    private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";
    private static string Number(double value) => value.ToString("0.######",CultureInfo.InvariantCulture);
    public string? ExportSchematic(string path)
    {
        try
        {
            var drawing = ProjectDrawing.Create(mockup,schematic,WorkspaceKind.Schematic);
            var bounds = drawing.Bounds; if (bounds.IsEmpty) bounds = new Rect(0,0,100,100);
            bounds.Inflate(ImageExportService.MarginPixels,ImageExportService.MarginPixels);
            var svg = new XElement(Ns+"svg",new XAttribute("viewBox",$"{Number(bounds.X)} {Number(bounds.Y)} {Number(bounds.Width)} {Number(bounds.Height)}"),
                new XAttribute("width",Number(bounds.Width)),new XAttribute("height",Number(bounds.Height)));
            svg.Add(ConvertDrawing(drawing));
            return repository.Write(path,new UTF8Encoding(false).GetBytes(new XDocument(svg).ToString()));
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or NotSupportedException)
        { return "Ошибка экспорта SVG: " + e.Message; }
    }
    private static XElement ConvertDrawing(Drawing drawing)
    {
        if (drawing is DrawingGroup group)
        {
            var element = new XElement(Ns+"g");
            var m = group.Transform?.Value ?? Matrix.Identity;
            if (!m.IsIdentity) element.SetAttributeValue("transform",$"matrix({Number(m.M11)} {Number(m.M12)} {Number(m.M21)} {Number(m.M22)} {Number(m.OffsetX)} {Number(m.OffsetY)})");
            if (group.Opacity != 1) element.SetAttributeValue("opacity",Number(group.Opacity));
            foreach (var child in group.Children) element.Add(ConvertDrawing(child));
            return element;
        }
        if (drawing is GlyphRunDrawing text) return Path(text.GlyphRun.BuildGeometry(),text.ForegroundBrush,null);
        if (drawing is GeometryDrawing shape) return Path(shape.Geometry,shape.Brush,shape.Pen);
        throw new NotSupportedException("Неподдерживаемый элемент SVG.");
    }
    private static XElement Path(Geometry geometry, Brush? fill, Pen? pen)
    {
        var path = PathGeometry.CreateFromGeometry(geometry).ToString(CultureInfo.InvariantCulture);
        if (path.StartsWith("F0",StringComparison.Ordinal) || path.StartsWith("F1",StringComparison.Ordinal)) path=path[2..].TrimStart();
        var element = new XElement(Ns+"path",new XAttribute("d",path),new XAttribute("fill-rule",geometry is PathGeometry pg && pg.FillRule == FillRule.Nonzero ? "nonzero" : "evenodd"),new XAttribute("fill",Color(fill)));
        if (fill is not null) element.SetAttributeValue("fill-opacity",Number(Opacity(fill)));
        if (pen is not null)
        {
            element.SetAttributeValue("stroke",Color(pen.Brush)); element.SetAttributeValue("stroke-width",Number(pen.Thickness));
            element.SetAttributeValue("stroke-opacity",Number(Opacity(pen.Brush))); element.SetAttributeValue("stroke-linejoin","round");
        }
        return element;
    }
    private static string Color(Brush? brush) => brush is SolidColorBrush b ? $"#{b.Color.R:X2}{b.Color.G:X2}{b.Color.B:X2}" : "none";
    private static double Opacity(Brush brush) => brush is SolidColorBrush b ? b.Opacity*b.Color.A/255 : brush.Opacity;
}

