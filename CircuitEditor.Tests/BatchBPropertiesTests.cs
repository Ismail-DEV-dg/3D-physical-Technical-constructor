using System.Text.Json;
using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Tests;

public sealed class BatchBPropertiesTests
{
    [Theory]
    [InlineData("capacitor", 10000000, "10 μF")]
    [InlineData("inductor", 2200, "2.2 mH")]
    public void NominalSelectionUpdatesBothRepresentationsImmediately(string id, long number, string text)
    {
        using var project = TestProjectFactory.Create();
        var item = project.Add(project.Library.Single(item => item.Id == id))!;
        var before = project.Revision;
        item.NominalEditor!.SelectedOption = item.NominalEditor.Options.Single(option => option.Value == number);
        Assert.Equal(text, item.Model.Value); Assert.Equal(text, item.EditValue); Assert.True(project.Revision > before);
        item.NominalEditor.SelectedOption = item.NominalEditor.Options[^1];
        item.NominalEditor.CustomText = "wrong"; item.NominalEditor.ApplyCustomCommand.Execute(null);
        Assert.NotEmpty(item.NominalEditor.Error); Assert.Equal(text, item.Model.Value);
    }
    [Fact]
    public void CustomNominalsStayInListAndCapacitorStyleKeepsTheValue()
    {
        using var project = TestProjectFactory.Create();
        var cap = project.Add(project.Library.Single(item => item.Id == "capacitor"))!;
        var editor = cap.NominalEditor!;
        editor.SelectedOption = editor.Options[^1]; editor.CustomText = "3,33 мкФ";
        editor.ApplyCustomCommand.Execute(null);
        Assert.Empty(editor.Error); Assert.Equal("3.33 μF", cap.Model.Value);
        Assert.Equal(3330000m, editor.SelectedOption!.Value);
        cap.SelectedCapacitorKind = CapacitorKind.Electrolytic;
        editor.SelectedOption = editor.Options.Single(option => option.Value == 47000000m);
        Assert.Contains(editor.Options, option => option.Value == 3330000m);
        Assert.Null(editor.Options[^1].Value);
        cap.SelectedCapacitorKind = CapacitorKind.Ceramic;
        Assert.Equal("47 μF", cap.Model.Value);
    }
    [Fact]
    public void MechanicalStopsAndPercentMappingAreConsistent()
    {
        Assert.Equal(0, PotentiometerPosition.ToPercent(-135)); Assert.Equal(100, PotentiometerPosition.ToPercent(135));
        Assert.Equal(0, PotentiometerPosition.FromPercent(50));
        Assert.Equal(135, PotentiometerPosition.FromPointer(0, 1, 90));
        Assert.Equal(-135, PotentiometerPosition.FromPointer(0, 1, -90));
        Assert.Equal(25, PotentiometerPosition.FromPointer(0, 0, 25));
    }
    [Fact]
    public void NewPropertiesRoundTripAndInvalidChangesAreRejected()
    {
        var project = BreadboardTestData.Create(); var cap = BreadboardTestData.Add(project, "capacitor");
        var pot = BreadboardTestData.Add(project, "potentiometer");
        Assert.True(cap.TrySetCapacitorKind(CapacitorKind.Electrolytic));
        Assert.True(pot.TrySetPotentiometerAngle(67.5));
        var json = JsonSerializer.Serialize(project); var restored = JsonSerializer.Deserialize<CircuitProject>(json)!;
        Assert.Equal(CapacitorKind.Electrolytic, restored.Circuit.Components.Single(item => item.Id == cap.Id).CapacitorKind);
        Assert.Equal(67.5, restored.Circuit.Components.Single(item => item.Id == pot.Id).PotentiometerAngle);
        Assert.False(pot.TrySetPotentiometerAngle(double.NaN)); Assert.False(pot.TrySetPotentiometerAngle(136));
        Assert.False(pot.TrySetCapacitorKind(CapacitorKind.Electrolytic));
        Assert.Equal(json, JsonSerializer.Serialize(project));
    }
    [Theory]
    [InlineData(0)] [InlineData(90)] [InlineData(180)] [InlineData(270)]
    public void MouseTurnsKnobAtEveryBoardRotationAndCancelRestoresValue(int rotation)
    {
        using var project = TestProjectFactory.Create(); using var scene = TestProjectFactory.Mockup(project);
        var item = project.Add(project.Library.Single(item => item.Id == "potentiometer"))!;
        while (item.MockupPlacement.Rotation != rotation) item.MockupPlacement.Rotate();
        item.MockupPlacement.MoveTo(30, 30);
        var knob = item.Footprint.RotaryControl!;
        var center = ContactGeometry.World(item.Footprint, new("knob", "W", knob.X, knob.Y), item.MockupPlacement, 2.54);
        var radians = rotation * Math.PI / 180;
        var x = PhysicalSettings.ToPixels(center.X) + scene.OffsetX;
        var y = PhysicalSettings.ToPixels(center.Y) + scene.OffsetY;
        scene.InputCommand.Execute(new(CanvasAction.Down, x, y));
        scene.InputCommand.Execute(new(CanvasAction.Move, x + Math.Cos(radians) * 12, y + Math.Sin(radians) * 12));
        Assert.Equal(90, item.Model.PotentiometerAngle, 6);
        Assert.Equal(30, item.MockupPlacement.X); Assert.Equal(30, item.MockupPlacement.Y);
        scene.InputCommand.Execute(new(CanvasAction.Cancel));
        Assert.Equal(0, item.Model.PotentiometerAngle);
        item.PotentiometerEditor!.PercentText = "100"; item.PotentiometerEditor.ApplyPercentCommand.Execute(null);
        Assert.Equal(135, item.Model.PotentiometerAngle); Assert.Equal(100, item.PotentiometerEditor.Percent);
        item.PotentiometerEditor.PercentText = "101"; item.PotentiometerEditor.ApplyPercentCommand.Execute(null);
        Assert.Equal(135, item.Model.PotentiometerAngle); Assert.NotEmpty(item.PotentiometerEditor.Error);
    }
}
