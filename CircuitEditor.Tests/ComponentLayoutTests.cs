using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Tests;

public sealed class ComponentLayoutTests
{
    public static IEnumerable<object[]> ComponentIds => new JsonComponentLibraryRepository().Load().Components
        .Select(definition => new object[] { definition.Id });

    [Theory]
    [MemberData(nameof(ComponentIds))]
    public void EveryComponentHasDisjointUnclippedLabelsAndGridAlignedPins(string id)
    {
        var definition = new JsonComponentLibraryRepository().Load().Components.Single(item => item.Id == id);
        var calculator = new ComponentLayoutCalculator(new TestTextMeasurer());
        var model = new Component(definition, definition.Prefix + "99999999");
        LayoutAssertions.Validate(model, calculator.Calculate(model));
        Assert.True(model.TryUpdateProperties(new string('W', Component.MaximumValueLength),
            "#39FF88", model.I2cAddress, out _));
        var expanded = calculator.Calculate(model);
        LayoutAssertions.Validate(model, expanded);
        Assert.True(expanded.Width >= calculator.Calculate(new Component(definition, "X1")).Width);
    }

    [Fact]
    public void PinPitchIsIdenticalForAllComponentsAndTitlesAreAlwaysPresent()
    {
        var calculator = new ComponentLayoutCalculator(new TestTextMeasurer());
        var library = new JsonComponentLibraryRepository().Load().Components;
        var layouts = library.Select(definition => calculator.Calculate(new Component(definition, definition.Prefix + "1"))).ToArray();
        Assert.Single(layouts.Select(layout => layout.PinPitch).Distinct());
        for (var index = 0; index < library.Count; index++)
        {
            Assert.Contains(layouts[index].Texts, text => text.Text == library[index].Name);
            Assert.Contains(layouts[index].Texts, text => text.Text == library[index].DefaultValue);
        }
    }

    [Fact]
    public void GalleryContainsAllComponentsInDisjointCellsAndPreservesExistingItems()
    {
        var model = TestProjectFactory.Schematic();
        var original = model.AddComponent(model.Library.First(), 100, 100);
        model.ShowAllComponentsCommand.Execute(null);
        Assert.Equal(29, model.ComponentCount);
        Assert.Equal(100, original.SchematicPlacement.X);
        Assert.Equal(100, original.SchematicPlacement.Y);
        var gallery = model.Components.Skip(1).ToArray();
        Assert.Equal(28, gallery.Select(item => item.Model.Definition.Id).Distinct().Count());
        var rectangles = model.Components.Select(item =>
            new LayoutRect(item.SchematicPlacement.X, item.SchematicPlacement.Y, item.Layout.Width, item.Layout.Height)).ToArray();
        for (var first = 0; first < rectangles.Length; first++)
            for (var second = first + 1; second < rectangles.Length; second++)
                Assert.False(rectangles[first].Intersects(rectangles[second]));
    }
}
