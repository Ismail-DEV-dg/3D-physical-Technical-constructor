using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CircuitEditor.App.Controls;

public sealed class StartupAurora : FrameworkElement
{
    private const int FrameWidth = 512;
    private const int FrameHeight = 192;
    private const int Stride = FrameWidth * 4;
    private const int GradientSamples = 256;
    private const double FieldTop = .28;

    private static readonly Brush Halo = CreateHalo();
    private static readonly Vector4[] Violet = CreateGradient("#B596FA", "#483191", .53f);
    private static readonly Vector4[] Teal = CreateGradient("#7FECDD", "#1B7CA4", .78f);
    private static readonly Vector4[] Green = CreateGradient("#A9F5A9", "#23866B", .68f);

    private readonly WriteableBitmap image = new(FrameWidth, FrameHeight, 96, 96, PixelFormats.Pbgra32, null);
    private readonly Vector4[] light = new Vector4[FrameWidth * FrameHeight];
    private readonly byte[] pixels = new byte[FrameHeight * Stride];
    private double lastPhase = double.NaN;

    public static readonly DependencyProperty PhaseProperty = DependencyProperty.Register(
        nameof(Phase), typeof(double), typeof(StartupAurora),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Phase
    {
        get => (double)GetValue(PhaseProperty);
        set => SetValue(PhaseProperty, value);
    }

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        var phase = Phase;
        if (phase != lastPhase)
        {
            Array.Clear(light);
            AddRibbon(Violet, .72, .058, .28, phase + .25);
            AddRibbon(Teal, .84, .072, .30, phase);
            AddRibbon(Green, .93, .044, .20, phase + .62);
            WriteFrame();
            lastPhase = phase;
        }

        context.DrawRectangle(Halo, null, new Rect(RenderSize));
        context.DrawImage(image, new Rect(0, ActualHeight * FieldTop, ActualWidth, ActualHeight * (1 - FieldTop)));
    }

    private void AddRibbon(Vector4[] gradient, double baseY, double amplitude, double depth, double phase)
    {
        const double rowsPerUnit = FrameHeight / (1 - FieldTop);
        for (var column = 0; column < FrameWidth; column++)
        {
            var x = (column + .5) / FrameWidth;
            var crest = baseY + amplitude * (Math.Sin(x * 7.4 - phase * 2.8)
                + .27 * Math.Sin(x * 15.3 + phase * 1.7));
            var falloff = depth * (.81 + .17 * Math.Cos(x * 9.7 + phase)
                + .12 * Math.Sin(x * 26 + .35 * Math.Sin(x * 15)));
            var strength = (float)(.18 + .82 * Math.Clamp(.54 + .22 * Math.Sin(x * 11 - phase * 2)
                + .16 * Math.Cos(x * 42 + phase) + .035 * Math.Sin(x * 137 + phase), 0, 1));

            var top = (crest - falloff - FieldTop) * rowsPerUnit;
            var bottom = (crest + depth * .28 - FieldTop) * rowsPerUnit;
            var gradientPerRow = (gradient.Length - 1) / (bottom - top);
            var firstRow = Math.Max(0, (int)Math.Ceiling(top - .5));
            var lastRow = Math.Min(FrameHeight - 1, (int)Math.Floor(bottom - .5));

            for (var row = firstRow; row <= lastRow; row++)
            {
                var position = (row + .5 - top) * gradientPerRow;
                var index = Math.Clamp((int)position, 0, gradient.Length - 2);
                var ink = Vector4.Lerp(gradient[index], gradient[index + 1], (float)(position - index)) * strength;
                var pixel = row * FrameWidth + column;
                light[pixel] = ink + light[pixel] * (1 - ink.W);
            }
        }
    }

    private void WriteFrame()
    {
        for (var index = 0; index < light.Length; index++)
        {
            var color = light[index] * 255;
            var pixel = index * 4;
            pixels[pixel] = (byte)Math.Clamp(color.Z + .5f, 0, 255);
            pixels[pixel + 1] = (byte)Math.Clamp(color.Y + .5f, 0, 255);
            pixels[pixel + 2] = (byte)Math.Clamp(color.X + .5f, 0, 255);
            pixels[pixel + 3] = (byte)Math.Clamp(color.W + .5f, 0, 255);
        }

        image.WritePixels(new Int32Rect(0, 0, FrameWidth, FrameHeight), pixels, Stride, 0);
    }

    private static Brush CreateHalo()
    {
        var halo = new RadialGradientBrush
        {
            Center = new(.53, 1.05),
            GradientOrigin = new(.49, 1.02),
            RadiusX = .86,
            RadiusY = .67
        };
        halo.GradientStops.Add(new(Color.FromArgb(195, 33, 91, 80), 0));
        halo.GradientStops.Add(new(Color.FromArgb(50, 17, 68, 63), .65));
        halo.GradientStops.Add(new(Color.FromArgb(0, 17, 68, 63), 1));
        halo.Freeze();
        return halo;
    }

    private static Vector4[] CreateGradient(string crest, string tail, float opacity)
    {
        var top = (Color)ColorConverter.ConvertFromString(crest);
        var bottom = (Color)ColorConverter.ConvertFromString(tail);
        (float Position, Color Color, float Alpha)[] stops =
        [
            (0, bottom, 0), (.32f, bottom, .31f), (.68f, top, .57f),
            (.78f, top, 1), (.89f, bottom, .43f), (1, bottom, 0)
        ];
        var gradient = new Vector4[GradientSamples];
        var stop = 0;
        for (var i = 0; i < gradient.Length; i++)
        {
            var position = i / (gradient.Length - 1f);
            while (position > stops[stop + 1].Position) stop++;
            var (from, a, alphaA) = stops[stop];
            var (to, b, alphaB) = stops[stop + 1];
            var fraction = (position - from) / (to - from);
            var rgb = Vector3.Lerp(new(a.R, a.G, a.B), new(b.R, b.G, b.B), fraction) / 255;
            var alpha = (alphaA + (alphaB - alphaA) * fraction) * opacity;
            gradient[i] = new(rgb * alpha, alpha);
        }

        return gradient;
    }
}
