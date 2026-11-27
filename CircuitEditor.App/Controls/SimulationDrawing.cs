using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.App.Controls;

internal static class SimulationDrawing
{
    private static readonly Brush CurrentParticle = PhysicalDrawing.Ink("#FFF29B");
    private static readonly Color[] RgbColors = [Colors.OrangeRed, Colors.LimeGreen, Colors.DeepSkyBlue];
    private const double ThousandMilliseconds = 1_000_000d;
    private const double MinimumDisplayCurrentAmps = .0001;
    internal const int MaximumParticles = 120;

    internal static bool HasVisualChanges(SimulationState? before, SimulationState? after, bool voltage, bool current)
    {
        if (before is null || after is null) return before != after;
        if (!ReferenceEquals(before.Components, after.Components) &&
            (before.Components.Count != after.Components.Count || after.Components.Any(pair =>
                !before.Components.TryGetValue(pair.Key, out var old) || !SameAppearance(old, pair.Value)))) return true;
        if (current && before.Microseconds != after.Microseconds &&
            after.Wires.Values.Any(signal => Math.Abs(signal.Current ?? 0) >= MinimumDisplayCurrentAmps)) return true;
        return (voltage || current) && !ReferenceEquals(before.Wires, after.Wires) &&
            (before.Wires.Count != after.Wires.Count || after.Wires.Any(pair =>
                !before.Wires.TryGetValue(pair.Key, out var old) || voltage && old.Voltage != pair.Value.Voltage ||
                current && old.Current != pair.Value.Current));
    }

    private static bool SameAppearance(ComponentSignal old, ComponentSignal updated) =>
        old.Intensity == updated.Intensity && old.Angle == updated.Angle && old.Text == updated.Text &&
        old.Active == updated.Active && old.Segments == updated.Segments && old.Channels.AsSpan().SequenceEqual(updated.Channels.AsSpan()) &&
        old.Measurements?.GetValueOrDefault("SwitchPosition") == updated.Measurements?.GetValueOrDefault("SwitchPosition");

    internal static void Component(DrawingContext dc, ComponentViewModel item, ComponentSignal? signal, DisplayDrawingCache displays)
    {
        if (signal is null) return;
        var footprint = item.Footprint;
        var body = PhysicalDrawing.Rect(footprint.Body);
        var center = new Point(body.Left + body.Width / 2, body.Top + body.Height / 2);
        switch (footprint.Renderer)
        {
            case "arduino-uno" when signal.Intensity > 0:
                BuiltinLed(dc, ArduinoUnoDrawing.BuiltinLed);
                break;
            case "arduino-nano" when signal.Intensity > 0:
                BuiltinLed(dc, ArduinoNanoDrawing.BuiltinLed(footprint));
                break;
            case "led" when signal.Intensity > 0:
                var color = (Color)ColorConverter.ConvertFromString(item.Model.Color ?? item.Model.Definition.DefaultColor);
                Glow(dc, center + new Vector(0, -2), body.Width, color, signal.Intensity);
                break;
            case "rgb-led" when !signal.Channels.IsDefaultOrEmpty:
                for (var i = 0; i < Math.Min(RgbColors.Length, signal.Channels.Length); i++)
                    if (signal.Channels[i] > 0) Glow(dc, center + new Vector((i - 1) * .9, 0), body.Width * .8, RgbColors[i], signal.Channels[i]);
                break;
            case "lcd1602":
            case "lcd1602-i2c":
            case "oled-ssd1306":
                displays.Draw(dc, item.Model.Id, footprint, signal);
                break;
            case "seven-segment":
                SevenSegmentDrawing.DrawActive(dc, footprint, signal.Segments);
                break;
            case "button" when signal.Active:
                dc.DrawEllipse(PhysicalDrawing.Ink("#536376"), new Pen(Brushes.DeepSkyBlue, .25), center, body.Width * .28, body.Height * .28);
                break;
            case "switch" when signal.Measurements?.GetValueOrDefault("SwitchPosition") is double position:
                MiscComponentDrawing.SwitchControl(dc, footprint, position);
                break;
            case "buzzer" when signal.Active:
                dc.DrawEllipse(null, new Pen(Brushes.Gold, .4), center, body.Width * .42, body.Height * .42);
                dc.DrawEllipse(null, new Pen(Brushes.Gold, .26), center, body.Width * .28, body.Height * .28);
                break;
            case "relay" when signal.Active:
                dc.DrawEllipse(Brushes.SpringGreen, null, body.TopRight + new Vector(-1.5, 1.5), .6, .6);
                break;
        }
    }

    internal static int Flow(DrawingContext dc, IReadOnlyList<LayoutPoint> points, double amps, long microseconds, int budget)
    {
        if (budget <= 0 || Math.Abs(amps) < MinimumDisplayCurrentAmps) return 0;
        var count = Math.Min(3, budget);
        var phase = microseconds / ThousandMilliseconds * (amps > 0 ? 1 : -1);
        for (var i = 0; i < count; i++)
        {
            var t = phase + (double)i / count;
            t -= Math.Floor(t);
            var x = t * (points.Count - 1);
            var index = (int)x;
            var a = points[index];
            var b = points[Math.Min(points.Count - 1, index + 1)];
            var f = x - index;
            dc.DrawEllipse(CurrentParticle, null, new Point(a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f), .36, .36);
        }
        return count;
    }

    private static void BuiltinLed(DrawingContext dc, Point origin)
    {
        const double width = 1.5;
        const double height = .8;
        var center = origin + new Vector(width / 2, height / 2);
        Glow(dc, center, 3.4, Colors.Gold, 1);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#FFF2A2"), null, new Rect(origin, new Size(width, height)), .12, .12);
    }

    private static void Glow(DrawingContext dc, Point position, double radius, Color color, double level)
    {
        color.A = (byte)(210 * Math.Sqrt(level));
        dc.DrawEllipse(new RadialGradientBrush(color, Color.FromArgb(0, color.R, color.G, color.B)), null,
            position, radius, radius);
    }

    internal static void Display(DrawingContext dc, Rect body, string text, bool oled)
    {
        var screen = oled ? new Rect(body.Left + 1.5, body.Top + 7.5, body.Width - 3, 16)
            : new Rect(body.Left + 7.2, body.Top + 11.9, body.Width - 14.4, 15.6);
        dc.DrawRectangle(PhysicalDrawing.Ink(oled ? "#071721" : "#264FC7"), null, screen);
        dc.PushClip(new RectangleGeometry(screen));
        var step = oled ? 1.7 : 6.1;
        var y = screen.Top + (oled ? 1.5 : 1.2);
        foreach (var line in text.Split('\n'))
        {
            PhysicalDrawing.Text(dc, line, screen.Left + 1, y, oled ? 1.4 : 4.1, oled ? Brushes.LightCyan : Brushes.WhiteSmoke);
            y += step;
        }
        dc.Pop();
    }
}
