using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrandingGenerator;

internal static class IconWriter
{
    public static void Write(string path, IReadOnlyList<(int Size, BitmapSource Image)> frames)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)frames.Count);
        var encoded = frames.Select(frame => frame.Size <= 64 ? Dib(frame.Image) : Png(frame.Image)).ToArray();
        var offset = 6 + 16 * frames.Count;
        for (var i = 0; i < frames.Count; i++)
        {
            var size = frames[i].Size;
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(encoded[i].Length);
            writer.Write(offset);
            offset += encoded[i].Length;
        }
        foreach (var bytes in encoded) writer.Write(bytes);
    }

    private static byte[] Png(BitmapSource image)
    {
        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static byte[] Dib(BitmapSource image)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var width = image.PixelWidth;
        var height = image.PixelHeight;
        var stride = width * 4;
        var maskStride = ((width + 31) / 32) * 4;
        var pixels = new byte[stride * height];
        new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0).CopyPixels(pixels, stride, 0);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixels.Length + maskStride * height);
        for (var i = 0; i < 4; i++) writer.Write(0);
        for (var y = height - 1; y >= 0; y--) writer.Write(pixels, y * stride, stride);
        var mask = new byte[maskStride];
        for (var y = height - 1; y >= 0; y--)
        {
            Array.Clear(mask);
            for (var x = 0; x < width; x++)
                if (pixels[y * stride + x * 4 + 3] == 0) mask[x / 8] |= (byte)(0x80 >> (x % 8));
            writer.Write(mask);
        }
        return stream.ToArray();
    }
}
