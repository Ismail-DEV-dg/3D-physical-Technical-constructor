using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.App.Controls;

public sealed class InstrumentTrace : FrameworkElement
{
    private const int Divisions = 8;
    private const int LogicChannels = 8;
    private const double MicrosPerMillisecond = 1000;
    private static readonly Brush[] BrushesByChannel = new[] { "#00CAE8", "#FFB51A", "#39E987", "#ED88FF", "#FF627E", "#A5ADFF", "#F5995D", "#9FEB39" }
        .Select(PhysicalDrawing.Ink).ToArray();

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(InstrumentTrace),
            new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SignalProperty = Register<InstrumentSignal?>(nameof(Signal), null);
    public static readonly DependencyProperty KindProperty = Register(nameof(Kind), InstrumentKind.Scope);
    public static readonly DependencyProperty VoltsProperty = Register(nameof(Volts), 1d);
    public static readonly DependencyProperty TimeProperty = Register(nameof(Time), 5d);
    public static readonly DependencyProperty TriggerModeProperty = Register(nameof(TriggerMode), 0);
    public static readonly DependencyProperty TriggerLevelProperty = Register(nameof(TriggerLevel), 2d);

    public InstrumentSignal? Signal { get => (InstrumentSignal?)GetValue(SignalProperty); set => SetValue(SignalProperty, value); }
    public InstrumentKind Kind { get => (InstrumentKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public double Volts { get => (double)GetValue(VoltsProperty); set => SetValue(VoltsProperty, value); }
    public double Time { get => (double)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }
    public int TriggerMode { get => (int)GetValue(TriggerModeProperty); set => SetValue(TriggerModeProperty, value); }
    public double TriggerLevel { get => (double)GetValue(TriggerLevelProperty); set => SetValue(TriggerLevelProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        var rect = new Rect(RenderSize);
        if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0) return;
        dc.DrawRectangle(PhysicalDrawing.Ink("#0B1420"), null, rect);
        var grid = new Pen(PhysicalDrawing.Ink("#2C4359"), 1);
        for (var i = 1; i < Divisions; i++)
        {
            var x = i * rect.Width / Divisions;
            var y = i * rect.Height / Divisions;
            dc.DrawLine(grid, new Point(x, 0), new Point(x, rect.Height));
            dc.DrawLine(grid, new Point(0, y), new Point(rect.Width, y));
        }
        var history = Signal?.History ?? [];
        if (history.IsDefaultOrEmpty) return;

        var width = Math.Max(1, Time * MicrosPerMillisecond * Divisions);
        var end = history[^1].Microseconds;
        var start = end - width;
        if (Kind == InstrumentKind.Scope && TriggerMode is 1 or 2 &&
            FindTrigger(history, end - width * .75) is long trigger)
            start = trigger - width * .25;
        var visible = history.Where(s => s.Microseconds >= start - SessionSampleMargin && s.Microseconds <= start + width).ToArray();
        dc.PushClip(new RectangleGeometry(rect));
        var channels = Kind == InstrumentKind.Scope ? 2 : LogicChannels;
        for (var channel = 0; channel < channels; channel++)
            PaintChannel(dc, visible, channel, rect, start, width);
        dc.Pop();
    }

    private const int SessionSampleMargin = 100;

    private long? FindTrigger(ImmutableArray<TraceSample> samples, double latest)
    {
        for (var i = samples.Length - 1; i > 0; i--)
        {
            var b = samples[i];
            if (b.Microseconds > latest || b.Voltages[0] is not double end ||
                samples[i - 1].Voltages[0] is not double start) continue;
            if (TriggerMode == 1 ? start < TriggerLevel && end >= TriggerLevel : start > TriggerLevel && end <= TriggerLevel)
                return b.Microseconds;
        }
        return null;
    }

    private void PaintChannel(DrawingContext dc, IReadOnlyList<TraceSample> samples, int channel, Rect rect, double start, double width)
    {
        var scope = Kind == InstrumentKind.Scope;
        var color = BrushesByChannel[channel];
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            Point? previous = null;
            foreach (var s in samples)
            {
                double? y = scope ? (rect.Height * .875 - s.Voltages[channel] * rect.Height / (Divisions * Volts))
                    : s.Levels[channel] switch { "1" => .22, "0" => .77, "Z" => .5, "X" => .5, _ => (double?)null };
                if (y is null) { previous = null; continue; }
                var position = new Point((s.Microseconds - start) / width * rect.Width,
                    scope ? y.Value : (channel + y.Value) * rect.Height / LogicChannels);
                if (previous is { } last)
                {
                    if (!scope) path.LineTo(new Point(position.X, last.Y), true, false);
                    path.LineTo(position, true, false);
                }
                else path.BeginFigure(position, false, false);
                previous = position;
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(color, 1.35), geometry);
    }
}
