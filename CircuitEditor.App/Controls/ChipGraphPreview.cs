using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Chips;

namespace CircuitEditor.App.Controls;

public sealed class ChipGraphPreview : FrameworkElement
{
    private const double Lane = 38;
    private const double BlockWidth = 80;
    private const double BlockHeight = 28;
    private static readonly Brush Face = PhysicalDrawing.Ink("#21344A");
    private static readonly Brush Rail = PhysicalDrawing.Ink("#345C77");
    private static readonly Brush Label = PhysicalDrawing.Ink("#E5F5FF");
    private static readonly Brush Wire = PhysicalDrawing.Ink("#60CFDC");

    public static readonly DependencyProperty GraphProperty = DependencyProperty.Register(nameof(Graph), typeof(ChipGraph),
        typeof(ChipGraphPreview), new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public ChipGraph? Graph { get => (ChipGraph?)GetValue(GraphProperty); set => SetValue(GraphProperty, value); }

    protected override Size MeasureOverride(Size available)
    {
        var names = Graph is { } graph ? Sources(graph).Count : 1;
        var rows = Math.Max(names, Math.Max(Graph?.Gates.Count ?? 0, Graph?.Outputs.Count ?? 0));
        return new(double.IsInfinity(available.Width) ? 420 : Math.Max(available.Width, 310), Math.Max(70, rows * Lane + 8));
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (Graph is not { } graph) return;
        var inputs = Sources(graph);
        var centerX = (ActualWidth - BlockWidth) / 2;
        var rightX = ActualWidth - BlockWidth - 8;
        var gatePins = graph.Gates.Select((gate, row) => (gate.Id, Point: new Point(centerX + BlockWidth, Row(row))))
            .ToDictionary(item => item.Id, item => item.Point);
        var inputPins = inputs.Select((source, row) => (source, Point: new Point(BlockWidth + 8, Row(row))))
            .ToDictionary(item => item.source, item => item.Point);
        Point? Find(ChipSignal source) => source.Gate is int gate
            ? gatePins.TryGetValue(gate, out var node) ? node : null
            : inputPins.TryGetValue(source, out var point) ? point : null;

        var pen = new Pen(Wire, 1);
        for (var row = 0; row < graph.Gates.Count; row++)
        {
            var ports = GateInputs(graph.Gates[row]).ToArray();
            for (var index = 0; index < ports.Length; index++)
                Link(dc, pen, Find(ports[index]), new Point(centerX, Row(row) - BlockHeight / 2
                    + (index + 1) * BlockHeight / (ports.Length + 1)));
        }
        for (var row = 0; row < graph.Outputs.Count; row++)
            Link(dc, pen, Find(graph.Outputs[row].Source), new Point(rightX, Row(row)));

        for (var row = 0; row < inputs.Count; row++)
        {
            var source = inputs[row];
            Block(dc, 8, Row(row), source.Pin is int pin ? $"P{pin}" : ChipTruthLogic.Symbol(source.Constant!.Value).ToString());
        }
        for (var row = 0; row < graph.Gates.Count; row++)
            Block(dc, centerX, Row(row), $"B{graph.Gates[row].Id} · {graph.Gates[row].Kind.ToString().ToUpperInvariant()}");
        for (var row = 0; row < graph.Outputs.Count; row++)
            Block(dc, rightX, Row(row), $"P{graph.Outputs[row].Pin} →");
    }

    private static IReadOnlyList<ChipSignal> Sources(ChipGraph graph) =>
        graph.Gates.SelectMany(GateInputs).Concat(graph.Outputs.Select(link => link.Source))
            .Where(source => source.Gate is null)
            .Distinct().OrderBy(source => source.Pin ?? int.MaxValue).ThenBy(source => source.Constant).ToArray();

    private static IEnumerable<ChipSignal> GateInputs(ChipGate gate) =>
        gate.Inputs.Concat(new[] { gate.Clear, gate.Preset }.OfType<ChipSignal>());

    private static double Row(int index) => (index + .5) * Lane;

    private static void Block(DrawingContext dc, double x, double y, string label)
    {
        dc.DrawRoundedRectangle(Face, new Pen(Rail, 1),
            new Rect(x, y - BlockHeight / 2, BlockWidth, BlockHeight), 4, 4);
        PhysicalDrawing.Text(dc, label, x + 8, y - 8, 11, Label);
    }

    private static void Link(DrawingContext dc, Pen pen, Point? source, Point target)
    {
        if (source is not { } start) return;
        var distance = Math.Max(24, Math.Abs(target.X - start.X) / 2);
        var path = new StreamGeometry();
        using (var context = path.Open())
        {
            context.BeginFigure(start, false, false);
            context.BezierTo(new Point(start.X + distance, start.Y), new Point(target.X - distance, target.Y), target, true, false);
        }
        dc.DrawGeometry(null, pen, path);
        dc.DrawEllipse(Wire, null, target, 2.3, 2.3);
    }
}
