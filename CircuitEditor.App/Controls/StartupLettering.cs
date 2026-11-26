using System.Windows;
using System.Windows.Media;

namespace CircuitEditor.App.Controls;

public sealed class StartupLettering : FrameworkElement
{
    private const double ArtworkWidth = 318;
    private const double ArtworkHeight = 78;
    private const double StrokeWidth = 2.7;

    // Centerlines are ordered as pen strokes. The short strokes place the dots and crosses.
    private static readonly (Point[] Points, double[] Times)[] Strokes =
    [
        CreateStroke("M 42,25 C 47,15 38,11 29,15 C 18,19 6,34 8,47 C 10,59 23,62 38,50 C 41,47 44,43 46,41"),
        CreateStroke("M 46,41 C 45,45 41,52 45,54 C 48,55 53,50 57,45"),
        CreateStroke("M 49,34 L 49.5,33"),
        CreateStroke("M 55,49 Q 58,44 59,39 Q 61,40 61,44 C 65,40 67,39 69,42 C 71,45 66,50 65,52 Q 67,57 76,46"),
        CreateStroke("M 76,46 C 84,39 84,35 80,37 C 74,40 69,53 77,54 C 82,55 87,50 91,45"),
        CreateStroke("M 91,45 Q 94,40 96,39 C 93,46 88,53 92,55 C 95,57 101,50 105,41 C 102,47 100,55 104,55 Q 109,54 114,45"),
        CreateStroke("M 114,45 L 117,40 C 115,47 110,54 114,55 Q 120,55 125,46"),
        CreateStroke("M 120,33 L 120.4,32.5"),
        CreateStroke("M 125,46 Q 131,38 135,26 C 129,39 120,54 126,56 C 132,58 140,49 145,47"),
        CreateStroke("M 123,37 Q 130,36 140,36"),
        CreateStroke("M 184,24 C 191,14 183,11 174,16 C 165,21 163,29 171,32 C 156,35 150,44 151,51 C 153,63 172,61 187,49 Q 192,45 194,43"),
        CreateStroke("M 194,43 C 187,38 178,54 185,56 C 193,57 200,43 205,28 C 209,17 207,13 203,18 C 199,23 193,39 192,48 C 190,59 199,56 208,46"),
        CreateStroke("M 208,46 L 211,40 C 209,47 205,54 209,55 Q 215,55 220,46"),
        CreateStroke("M 214,33 L 214.4,32.5"),
        CreateStroke("M 220,46 Q 226,38 230,27 C 224,39 216,54 222,56 C 228,57 234,49 237,45"),
        CreateStroke("M 219,37 Q 226,36 236,36"),
        CreateStroke("M 237,45 C 242,38 248,37 248,43 C 249,48 242,57 237,54 C 230,50 239,37 245,40 C 247,44 251,46 256,44"),
        CreateStroke("M 255,47 Q 258,43 258,39 Q 261,40 260,44 C 264,41 267,39 270,41 C 272,43 266,51 269,54 C 272,58 280,51 285,48 Q 295,40 305,43"),
        CreateStroke("M 31,68 C 83,58 134,62 169,65 C 207,69 234,68 253,64")
    ];

    private static readonly double TotalTime = Strokes.Sum(stroke => stroke.Times[^1]);
    private static readonly Pen Ink = CreatePen();

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(StartupLettering),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (Progress <= 0 || RenderSize.Width <= 0 || RenderSize.Height <= 0) return;

        var scale = Math.Min(RenderSize.Width / ArtworkWidth, RenderSize.Height / ArtworkHeight);
        context.PushTransform(new MatrixTransform(scale, 0, 0, scale,
            (RenderSize.Width - ArtworkWidth * scale) / 2, (RenderSize.Height - ArtworkHeight * scale) / 2));

        var time = Math.Clamp(Progress, 0, 1) * TotalTime;
        foreach (var stroke in Strokes)
        {
            if (time <= 0) break;
            DrawStroke(context, stroke.Points, stroke.Times, time);
            time -= stroke.Times[^1];
        }

        context.Pop();
    }

    private static void DrawStroke(DrawingContext context, Point[] points, double[] times, double time)
    {
        var drawing = new StreamGeometry();
        using (var line = drawing.Open())
        {
            line.BeginFigure(points[0], false, false);
            for (var i = 1; i < points.Length; i++)
            {
                if (time < times[i])
                {
                    var fraction = (time - times[i - 1]) / (times[i] - times[i - 1]);
                    line.LineTo(points[i - 1] + (points[i] - points[i - 1]) * fraction, true, true);
                    break;
                }

                line.LineTo(points[i], true, true);
            }
        }

        context.DrawGeometry(null, Ink, drawing);
    }

    private static (Point[] Points, double[] Times) CreateStroke(string path)
    {
        var figure = Geometry.Parse(path).GetFlattenedPathGeometry(.08, ToleranceType.Absolute).Figures[0];
        var points = new List<Point> { figure.StartPoint };
        foreach (var segment in figure.Segments)
        {
            if (segment is PolyLineSegment polyline) points.AddRange(polyline.Points);
            else if (segment is LineSegment line) points.Add(line.Point);
        }

        var distinctPoints = new List<Point> { points[0] };
        foreach (var point in points.Skip(1))
            if ((point - distinctPoints[^1]).LengthSquared > .0001) distinctPoints.Add(point);

        var times = new double[distinctPoints.Count];
        for (var i = 1; i < distinctPoints.Count; i++)
        {
            var step = distinctPoints[i] - distinctPoints[i - 1];
            var bend = i + 1 < distinctPoints.Count
                ? Math.Abs(Vector.AngleBetween(step, distinctPoints[i + 1] - distinctPoints[i])) : 0;
            times[i] = times[i - 1] + step.Length * (1 + Math.Min(bend / 55, .8));
        }

        return (distinctPoints.ToArray(), times);
    }

    private static Pen CreatePen()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(249, 248, 245)), StrokeWidth)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }
}
