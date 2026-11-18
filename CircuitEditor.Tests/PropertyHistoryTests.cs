using CircuitEditor.Core.History;
using CircuitEditor.Core.Models;
namespace CircuitEditor.Tests;
public sealed class PropertyHistoryTests
{
    [Theory]
    [InlineData("capacitor")][InlineData("inductor")][InlineData("potentiometer")][InlineData("dht11")]
    [InlineData("switch")][InlineData("battery")][InlineData("led")][InlineData("lcd1602-i2c")]
    public void AllPropertyEditorsParticipateInHistory(string type)
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var c=p.Add(p.Library.Single(d=>d.Id==type))!;
        p.History.Clear(); var before=new ProjectSnapshot(p.Model);
        switch(type)
        {
            case "capacitor": c.SelectedCapacitorKind=CapacitorKind.Electrolytic; break;
            case "inductor": c.NominalEditor!.SelectedOption=c.NominalEditor.Options.First(o=>o.Text.Contains("100")); break;
            case "potentiometer": c.PotentiometerEditor!.SetAngle(45); break;
            case "dht11": c.InputsEditor!.Parameters[0].Value=35; break;
            case "switch": c.InputsEditor!.Parameters[0].Value=1; break;
            case "battery": c.InputsEditor!.Parameters[0].Value=3.7; break;
            case "led": c.EditColor="#112233"; p.ApplyPropertiesCommand.Execute(null); break;
            case "lcd1602-i2c": c.EditI2cAddress="0x3f"; p.ApplyPropertiesCommand.Execute(null); break;
        }
        Assert.Equal(1,p.History.Count); var after=new ProjectSnapshot(p.Model);
        p.UndoCommand.Execute(null); Assert.True(before.Matches(p.Model)); p.RedoCommand.Execute(null); Assert.True(after.Matches(p.Model));
    }
    [Fact]
    public void EachTypeHasOnlyRelevantEditableFieldsAndColorOnlyOnLed()
    {
        using var e=new Stage4TestEnvironment();
        foreach(var definition in e.Project.Library)
        {
            var c=e.Project.Add(definition)!;
            Assert.Equal(definition.Id=="led",c.ShowsColor);
            Assert.Equal(definition.Id=="led",c.Model.Color is not null);
            Assert.Equal(definition.DefaultI2cAddress is not null,c.SupportsI2c);
            if(definition.Id=="resistor") { Assert.False(c.ShowsVoltage); Assert.False(c.ShowsGenericValue); }
        }
    }
}
