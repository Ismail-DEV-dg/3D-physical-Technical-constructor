using CircuitEditor.Core.Layout;

namespace CircuitEditor.Tests;

internal sealed class TestTextMeasurer : ITextMeasurer
{
    public TextExtent Measure(string text, double fontSize) => new(text.Length * fontSize * 0.6 + 2, fontSize + 6);
}
