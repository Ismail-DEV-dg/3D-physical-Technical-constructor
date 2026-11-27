using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CircuitEditor.App.Controls;

internal static class StartupStarCloud
{
    private const int Width = 768;
    private const int Height = 320;
    private const double SkyHeight = .65;
    private static readonly Lazy<Task<BitmapSource>> Cloud = new(() => Task.Run(Create));

    internal static double Band(double x) => .46 - x * .42 + Math.Sin(x * 5.8) * .025;

    internal static void Prefetch() => _ = Cloud.Value;

    internal static void Draw(DrawingContext context, Size size)
    {
        if (Cloud.Value.IsCompletedSuccessfully)
            context.DrawImage(Cloud.Value.Result, new Rect(0, 0, size.Width, size.Height * SkyHeight));
    }

    private static BitmapSource Create()
    {
        byte[] pixels = new byte[Width * Height * 4];
        for (var row = 0; row < Height; row++)
            for (var column = 0; column < Width; column++)
            {
                var x = (column + .5) / Width;
                var y = (row + .5) / Height * SkyHeight;
                var center = Band(x);
                var wisps = Fractal(x * 12, y * 20);
                var dust = Fractal(x * 35 + 19, y * 61 - 8);
                var filaments = Fractal(x * 58 - 16, y * 93 + 3);
                var offset = y - center + (wisps - .5) * .096;
                var veil = Gaussian(offset, .082) * (.14 + wisps * .48);
                var arm = Gaussian(offset + .022 + (dust - .5) * .040, .029) * (.22 + wisps * .62);
                var junction = .56 + .44 * Gaussian(x - .48, .22);
                var gap = Gaussian(offset - .007 + .021 * Math.Sin(x * 26), .016) *
                    Math.Clamp((dust + filaments - .64) * 1.25, 0, .78);
                var light = Math.Clamp((veil * .19 + arm * .58 * junction) * (1 - gap) * (.50 + filaments * .83), 0, .68);
                light *= Math.Clamp((SkyHeight - y) / .16, 0, 1);
                var warmth = Math.Clamp(arm * junction * 1.5, 0, 1);
                var index = (row * Width + column) * 4;
                pixels[index] = (byte)(light * (185 - 17 * warmth));
                pixels[index + 1] = (byte)(light * (154 + 43 * warmth));
                pixels[index + 2] = (byte)(light * (132 + 90 * warmth));
                pixels[index + 3] = (byte)(light * 255);
            }
        var bitmap = BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Pbgra32, null, pixels, Width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static double Gaussian(double value, double width) => Math.Exp(-.5 * Math.Pow(value / width, 2));

    private static double Fractal(double x, double y) =>
        .54 * Noise(x, y) + .28 * Noise(x * 2.03 + 5, y * 2.03 + 12) +
        .12 * Noise(x * 4.1 - 9, y * 4.1) + .06 * Noise(x * 8.2, y * 8.2 - 7);

    private static double Noise(double x, double y)
    {
        var ix = (int)Math.Floor(x);
        var iy = (int)Math.Floor(y);
        var u = Smooth(x - ix);
        var v = Smooth(y - iy);
        var north = Mix(Hash(ix, iy), Hash(ix + 1, iy), u);
        var south = Mix(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), u);
        return Mix(north, south, v);
    }

    private static double Smooth(double t) => t * t * (3 - 2 * t);
    private static double Mix(double from, double to, double t) => from + (to - from) * t;

    private static double Hash(int x, int y)
    {
        var value = unchecked((uint)x * 0x9E3779B1u + (uint)y * 0x85EBCA77u);
        value = unchecked((value ^ (value >> 15)) * 0xC2B2AE3Du);
        return (value ^ (value >> 16)) / (double)uint.MaxValue;
    }
}
