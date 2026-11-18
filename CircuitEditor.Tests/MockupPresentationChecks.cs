using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Tests;

internal static class MockupPresentationChecks
{
    public static void Run(ResourceDictionary resources)
    {
        using var project = TestProjectFactory.Create(new CircuitEditor.App.Services.WpfTextMeasurer());
        using var mockup = TestProjectFactory.Mockup(project);
        var resistor = project.Add(project.Library.Single(item => item.Id == "resistor"))!;
        var board = mockup.Board!;
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/D/05");
        Assert.True(mockup.TryPlace(resistor, new ComponentPlacement(
            board.X + hole.Position.X, board.Y + hole.Position.Y, spanHoles: 8)));
        var combo = new TestComboBox { ItemsSource = mockup.SpanOptions };
        SelectionRefreshBehavior.SetEnabled(combo, true);
        combo.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(mockup.SelectedSpan))
        { Source = mockup, Mode = BindingMode.TwoWay });
        Assert.Equal(8, combo.SelectedItem);
        combo.SelectedItem = 9;
        combo.GetBindingExpression(Selector.SelectedItemProperty)!.UpdateSource();
        Assert.Equal(8, mockup.SelectedSpan);
        combo.FinishSelection();
        Assert.Equal(8, combo.SelectedItem);
        Assert.Equal(2, project.Model.Bindings.Count);
        foreach (var id in new[] { "arduino-uno", "led", "button", "diode", "oled-ssd1306" })
        {
            var item = project.Add(project.Library.Single(item => item.Id == id))!;
            item.MockupPlacement.MoveTo(5, 10 + project.ComponentCount * 10);
        }
        var surface = new BreadboardSurface { Resources = resources, Scene = mockup,
            Background = (Brush)resources["BackgroundBrush"] };
        surface.Measure(new Size(1000, 700));
        surface.Arrange(new Rect(0, 0, 1000, 700));
        surface.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1000, 700, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        Assert.Equal(1000, bitmap.PixelWidth);
    }
}
