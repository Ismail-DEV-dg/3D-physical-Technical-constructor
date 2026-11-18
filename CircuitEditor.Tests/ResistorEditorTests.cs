using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Resistance;

namespace CircuitEditor.Tests;

public sealed class ResistorEditorTests
{
    [Fact]
    public void SelectingNominalUpdatesSharedModelAndInvalidatesDrawingImmediately()
    {
        using var project = TestProjectFactory.Create();
        var item = project.Add(project.Library.Single(definition => definition.Id == "resistor"))!;
        var editor = item.ResistorEditor!;
        Assert.Equal(ResistorSeriesKind.E12, editor.SelectedSeries);
        Assert.False(item.ShowsColor); Assert.False(item.ShowsVoltage);
        var oldBands = ResistorMarking.GetBands(item.Model.Value);
        var revision = project.Revision;
        editor.SelectedOption = editor.Options.Single(option => option.Ohms == 4700m);
        Assert.Equal("4.7 kΩ", item.Model.Value);
        Assert.Equal("4.7 kΩ", editor.SelectedOption!.ToString());
        Assert.Equal(item.Model.Value, item.EditValue);
        Assert.NotEqual(oldBands, ResistorMarking.GetBands(item.Model.Value));
        Assert.True(project.Revision > revision);
        Assert.Same(item, project.SelectedComponent);
    }

    [Fact]
    public void CustomValueSurvivesSeriesChangesAndInvalidInputDoesNotChangeModel()
    {
        decimal actual = 220;
        var editor = new ResistorEditorViewModel("220 Ω", value => actual = value);
        editor.SelectedOption = editor.Options[^1];
        Assert.True(editor.IsCustomEntry);
        editor.CustomText = "20 MΩ";
        editor.ApplyCustomCommand.Execute(null);
        Assert.Equal(220m, actual); Assert.NotEmpty(editor.Error);
        editor.CustomText = "2,35 kΩ";
        editor.ApplyCustomCommand.Execute(null);
        Assert.Equal(2350m, actual); Assert.Empty(editor.Error);
        Assert.False(editor.IsCustomEntry);
        foreach (var series in Enum.GetValues<ResistorSeriesKind>())
        {
            editor.SelectedSeries = series;
            Assert.Equal(2350m, editor.SelectedOption!.Ohms);
            Assert.Single(editor.Options, option => option.Ohms == 2350m);
            Assert.Null(editor.Options[^1].Ohms);
        }
        editor.SelectedOption = editor.Options.Single(option => option.Ohms == 1000m);
        Assert.Equal(1000m, actual);
        Assert.Contains(editor.Options, option => option.Ohms == 2350m);
    }

    [Fact]
    public void CustomPrecisionIsPreservedInsteadOfSilentlyRoundingBands()
    {
        decimal actual = 220;
        var editor = new ResistorEditorViewModel("220 Ω", value => actual = value);
        editor.CustomText = "1.234 Ω";
        editor.ApplyCustomCommand.Execute(null);
        Assert.Equal(1.234m, actual);
        Assert.NotEmpty(editor.MarkingNote);
        editor.SelectedOption = editor.Options.Single(option => option.Ohms == 220m);
        Assert.Empty(editor.MarkingNote);
    }
}
