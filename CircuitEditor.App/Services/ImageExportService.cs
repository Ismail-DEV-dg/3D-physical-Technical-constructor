using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.App.Services;
public sealed class ImageExportService(BreadboardViewModel mockup, SchematicViewModel schematic, IExportRepository repository) : IImageExportService
{
    private const int MaximumDimension = 16384;
    private const long MaximumPixels = 64000000;
    internal const double MarginPixels = 24;
    public string? ExportPng(string path, WorkspaceKind kind, double scale)
    {
        try
        {
            if (kind is not (WorkspaceKind.Mockup or WorkspaceKind.Schematic) || scale is < .25 or > 4 || !double.IsFinite(scale)) return "Выберите Макет или Схему и допустимый масштаб.";
            var drawing = ProjectDrawing.Create(mockup,schematic,kind);
            var bounds = drawing.Bounds;
            if (bounds.IsEmpty) bounds = new Rect(0,0,100,100);
            bounds.Inflate(MarginPixels,MarginPixels);
            var width = (int)Math.Ceiling(bounds.Width*scale); var height = (int)Math.Ceiling(bounds.Height*scale);
            if (width > MaximumDimension || height > MaximumDimension || (long)width*height > MaximumPixels) return "Изображение слишком велико. Выберите меньший масштаб.";
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White,null,new Rect(0,0,width,height));
                dc.PushTransform(new ScaleTransform(scale,scale)); dc.PushTransform(new TranslateTransform(-bounds.X,-bounds.Y)); dc.DrawDrawing(drawing);
            }
            var bitmap = new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32); bitmap.Render(visual);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var memory = new MemoryStream(); encoder.Save(memory); return repository.Write(path,memory.ToArray());
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or NotSupportedException or OverflowException)
        { return "Ошибка экспорта PNG: " + e.Message; }
    }
}
