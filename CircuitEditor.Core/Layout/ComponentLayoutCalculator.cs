using CircuitEditor.Core.Geometry;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Layout;

/// <summary>Единый алгоритм раскладки без WPF и размеров отдельных библиотечных компонентов.</summary>
public sealed class ComponentLayoutCalculator(ITextMeasurer measurer) : IComponentLayoutCalculator
{
    /// <inheritdoc/>
    public ComponentLayout Calculate(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        var definition = component.Definition;
        var pins = definition.Pins.Where(pin => pin.IsExposed).OrderBy(Group).ToArray();
        var leftCount = (pins.Length + 1) / 2;
        var left = pins.Take(leftCount).ToArray();
        var right = pins.Skip(leftCount).ToArray();
        var metrics = pins.ToDictionary(pin => pin.Id, pin => Measure(pin.Name));
        double ColumnWidth(IEnumerable<Pin> column) => column.Select(pin => metrics[pin.Id].Width).DefaultIfEmpty().Max();
        var leftWidth = ColumnWidth(left);
        var rightWidth = ColumnWidth(right);
        var reference = Measure(component.Reference);
        var name = Measure(definition.Name);
        var value = Measure(component.Value);
        var addressText = component.I2cAddress is null ? "" : "I2C " + component.I2cAddress;
        var address = Measure(addressText);
        var pad = ComponentLayoutStyle.BodyPadding;
        var gap = ComponentLayoutStyle.TextGap;
        var lead = ComponentLayoutStyle.LeadLength;
        var clearance = ComponentLayoutStyle.PinTextClearance;
        var centralWidth = ComponentLayoutStyle.SymbolWidth + 2 * ComponentLayoutStyle.SymbolGap;
        var rowWidth = leftWidth + rightWidth + centralWidth + 2 * clearance;
        var headerWidth = reference.Width + (addressText.Length == 0 ? 0 : gap + address.Width);
        var bodyWidth = Math.Max(rowWidth, Math.Max(headerWidth, Math.Max(name.Width, value.Width)) + 2 * pad);
        // Even grid dimensions keep the rotation centre on the grid at every quarter turn.
        var width = RoundUp(bodyWidth + 2 * lead, 2 * DiagramGeometry.GridSize);
        var headerHeight = Math.Max(reference.Height, address.Height) + 2 * pad;
        var maxLabelHeight = Math.Max(Measure("Ag08l1").Height, metrics.Values.Select(size => size.Height).DefaultIfEmpty().Max());
        var pitch = RoundUp(maxLabelHeight + ComponentLayoutStyle.RowGap, DiagramGeometry.GridSize);
        var firstY = RoundUp(headerHeight + maxLabelHeight / 2, DiagramGeometry.GridSize);
        var rows = Math.Max(left.Length, right.Length);
        var rowAreaHeight = Math.Max(rows * pitch, ComponentLayoutStyle.SymbolHeight);
        var rowTop = firstY - pitch / 2;
        var rowBottom = rowTop + rowAreaHeight;
        var nameY = rowBottom + gap;
        var valueY = nameY + name.Height + gap;
        var height = RoundUp(valueY + value.Height + pad, 2 * DiagramGeometry.GridSize);
        var body = new LayoutRect(lead, 0, width - 2 * lead, height);
        var freeLeft = body.X + clearance + leftWidth + ComponentLayoutStyle.SymbolGap;
        var freeRight = body.Right - clearance - rightWidth - ComponentLayoutStyle.SymbolGap;
        var symbol = new LayoutRect((freeLeft + freeRight - ComponentLayoutStyle.SymbolWidth) / 2,
            rowTop + (rowAreaHeight - ComponentLayoutStyle.SymbolHeight) / 2,
            ComponentLayoutStyle.SymbolWidth, ComponentLayoutStyle.SymbolHeight);
        var resultPins = new List<PinLayout>();
        void AddColumn(Pin[] column, bool isLeft)
        {
            for (var index = 0; index < column.Length; index++)
            {
                var pin = column[index];
                var extent = metrics[pin.Id];
                var y = firstY + index * pitch;
                var x = isLeft ? body.X + clearance : body.Right - clearance - extent.Width;
                var label = new LayoutText(pin.Name, new LayoutRect(x, y - extent.Height / 2, extent.Width, extent.Height),
                    ComponentLayoutStyle.FontSize);
                resultPins.Add(new PinLayout(pin, new LayoutPoint(isLeft ? 0 : width, y), isLeft, label));
            }
        }
        AddColumn(left, true);
        AddColumn(right, false);
        var texts = new List<LayoutText>
        {
            Place(component.Reference, reference, body.X + pad, pad),
            Place(definition.Name, name, body.X + pad, nameY),
            Place(component.Value, value, body.X + pad, valueY)
        };
        if (addressText.Length > 0) texts.Add(Place(addressText, address, body.Right - pad - address.Width, pad));
        return new ComponentLayout(width, height, body, symbol, texts.AsReadOnly(), resultPins.AsReadOnly(), pitch);
    }

    private TextExtent Measure(string text)
    {
        var extent = measurer.Measure(text, ComponentLayoutStyle.FontSize);
        if (!double.IsFinite(extent.Width) || !double.IsFinite(extent.Height) || extent.Width < 0 || extent.Height <= 0)
            throw new InvalidOperationException("Измеритель вернул некорректные границы текста.");
        return extent;
    }

    private static LayoutText Place(string text, TextExtent extent, double x, double y) =>
        new(text, new LayoutRect(x, y, extent.Width, extent.Height), ComponentLayoutStyle.FontSize);
    private static double RoundUp(double value, double step) => Math.Ceiling(value / step) * step;
    private static int Group(Pin pin) => pin.Type switch
    {
        PinType.Power or PinType.Ground => 0,
        PinType.DigitalIO or PinType.PWM or PinType.UART_TX or PinType.UART_RX => 1,
        PinType.AnalogIn => 2,
        _ => 3
    };
}
