using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrandingGenerator;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var folder = Path.GetFullPath(args.FirstOrDefault() ?? "assets/branding");
        var name = args.ElementAtOrDefault(1) ?? "chip-trace";
        var selected = MarkCatalog.Candidates.SingleOrDefault(mark => mark.Name == name);
        if (selected is null)
        {
            Console.Error.WriteLine("Варианты: chip-trace, board-gate, pulse-loop.");
            return 1;
        }

        Directory.CreateDirectory(Path.Combine(folder, "candidates"));
        foreach (var mark in MarkCatalog.Candidates)
        {
            File.WriteAllText(Path.Combine(folder, "candidates", mark.Name + ".svg"), BrandSvg.Square(mark));
            Save(Path.Combine(folder, "candidates", mark.Name + ".png"), Square(mark, 256));
        }

        File.WriteAllText(Path.Combine(folder, "logo.svg"), BrandSvg.Square(selected));
        File.WriteAllText(Path.Combine(folder, "logo-horizontal.svg"), BrandSvg.Horizontal(selected));
        foreach (var size in new[] { 16, 24, 32 })
        {
            var design = MarkCatalog.Small(name, size);
            File.WriteAllText(Path.Combine(folder, $"logo-{size}.svg"), BrandSvg.Square(design));
        }
        foreach (var size in new[] { 256, 512 })
            Save(Path.Combine(folder, $"logo-{size}.png"), Square(selected, size));
        var frames = new[] { 16, 24, 32, 48, 64, 128, 256 }.Select(size =>
            (size, Square(size <= 32 ? MarkCatalog.Small(name, size) : selected, size))).ToArray();
        IconWriter.Write(Path.Combine(folder, "logo.ico"), frames);

        Save(Path.Combine(folder, "logo-horizontal.png"), BrandRenderer.Render(640, 147, ctx =>
        {
            ctx.DrawRoundedRectangle(BrandRenderer.Ink(MarkCatalog.Background), null, new Rect(0, 0, 640, 147), 26, 26);
            BrandRenderer.DrawMark(ctx, selected, 16, 16, 115);
            BrandRenderer.Label(ctx, "CIRCUIT / EDITOR", 153, 42, 48);
        }));
        Social(folder, selected);
        Wizard(folder, selected);
        return 0;
    }

    private static BitmapSource Square(Mark mark, int size) => BrandRenderer.Render(size, size,
        ctx => BrandRenderer.DrawMark(ctx, mark, 0, 0, size));

    private static void Social(string folder, Mark mark)
    {
        Save(Path.Combine(folder, "social-preview.png"), BrandRenderer.Render(1280, 640, ctx =>
        {
            BrandRenderer.Fill(ctx, 1280, 640);
            var grid = new Pen(BrandRenderer.Ink("#252746"), 1);
            for (var x = 40; x < 1280; x += 40) ctx.DrawLine(grid, new Point(x, 32), new Point(x, 608));
            for (var y = 48; y < 640; y += 40) ctx.DrawLine(grid, new Point(36, y), new Point(1244, y));
            ctx.DrawRoundedRectangle(BrandRenderer.Ink("#10192B"), new Pen(BrandRenderer.Ink("#36486E"), 2),
                new Rect(58, 103, 1164, 434), 30, 30);
            BrandRenderer.DrawMark(ctx, mark, 93, 139, 356);
            BrandRenderer.Label(ctx, "ARDUINO / ESP32", 501, 164, 25, MarkCatalog.Green);
            BrandRenderer.Label(ctx, "CIRCUIT", 495, 200, 90);
            BrandRenderer.Label(ctx, "/ EDITOR", 494, 308, 90, MarkCatalog.Cyan);
            BrandRenderer.Label(ctx, "Circuit design  ·  Simulation  ·  3D", 501, 445, 32, BrandRenderer.Muted, false);
        }));
    }

    private static void Wizard(string folder, Mark mark)
    {
        Save(Path.Combine(folder, "wizard-panel.bmp"), BrandRenderer.Render(164, 314, ctx =>
        {
            BrandRenderer.Fill(ctx, 164, 314);
            var board = new Pen(BrandRenderer.Ink("#313050"), 2);
            ctx.DrawGeometry(null, board, Geometry.Parse("M0 242 H37 L56 223 H164 M0 268 H59 L79 248 H164 M0 294 H81 L100 275 H164"));
            BrandRenderer.DrawMark(ctx, mark, 18, 36, 128);
            BrandRenderer.Label(ctx, "CIRCUIT", 18, 178, 24);
            BrandRenderer.Label(ctx, "/ EDITOR", 18, 205, 22, MarkCatalog.Cyan);
        }), bmp: true);
        Save(Path.Combine(folder, "wizard-icon.bmp"), BrandRenderer.Render(55, 55, ctx =>
        {
            BrandRenderer.Fill(ctx, 55, 55);
            BrandRenderer.DrawMark(ctx, mark, 2, 2, 51);
        }), bmp: true);
    }

    private static void Save(string path, BitmapSource bitmap, bool bmp = false)
    {
        BitmapEncoder encoder = bmp ? new BmpBitmapEncoder() : new PngBitmapEncoder();
        var source = bmp ? new FormatConvertedBitmap(bitmap, PixelFormats.Bgr24, null, 0) : bitmap;
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
