using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class DcSolverTests
{
    private static DcResult Solve(ElectricalTestCircuit e)=>new DcSolver(new JsonElectricalLibraryRepository()).Solve(e.Project.Model,e.Nets());
    [Fact]
    public void VoltageDividerHasExpectedNodeCurrentAndPower()
    {
        using var e=new ElectricalTestCircuit();var b=e.Add("battery");b.Model.TrySetInput(ComponentInput.BatteryVoltage,5);
        var r1=e.Add("resistor");var r2=e.Add("resistor");r1.Model.TryUpdateProperties("1 kΩ",null,null,out _);r2.Model.TryUpdateProperties("1 kΩ",null,null,out _);
        e.Wire(b,"+",r1,"1");e.Wire(r1,"2",r2,"1");e.Wire(r2,"2",b,"-");
        var result=Solve(e);Assert.True(result.Success,string.Join(";",result.Diagnostics));
        Assert.Equal(2.5,result.Voltages[e.Nets().Find(r1.Model.Id,"2")!.Id]!.Value,8);
        Assert.Equal(.0025,result.Branches.Single(x=>x.ComponentId==r1.Model.Id).Current!.Value,8);
        Assert.Equal(.00625,result.Branches.Single(x=>x.ComponentId==r1.Model.Id).Power!.Value,8);
    }
    [Fact]
    public void LedCurrentMatchesPiecewiseHandCalculationAndColorChange()
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var r=e.Add("resistor");var led=e.Add("led");
        e.Wire(uno,"5V",r,"1");e.Wire(r,"2",led,"A");e.Wire(led,"K",uno,"GND");
        var result=Solve(e);Assert.True(result.Success,string.Join(";",result.Diagnostics));
        Assert.Equal((5-2d)/(220+10),result.Branches.Single(x=>x.ComponentId==led.Model.Id).Current!.Value,8);
        led.Model.TryUpdateProperties(led.Model.Value,"#307EFF",null,out _);result=Solve(e);
        Assert.Equal((5-3d)/(220+10),result.Branches.Single(x=>x.ComponentId==led.Model.Id).Current!.Value,8);
    }
    [Theory]
    [InlineData(-135,5)][InlineData(0,2.5)][InlineData(135,0)]
    public void PotentiometerExtremesAreIdealAndFinite(double angle,double expected)
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var pot=e.Add("potentiometer");pot.Model.TrySetPotentiometerAngle(angle);
        e.Wire(uno,"5V",pot,"A");e.Wire(uno,"GND",pot,"B");var result=Solve(e);
        Assert.True(result.Success,string.Join(";",result.Diagnostics));Assert.Equal(expected,result.Voltages[e.Nets().Find(pot.Model.Id,"W")!.Id]!.Value,8);
    }
    [Fact]
    public void FloatingShortedAndUnsupportedCasesReturnDiagnostics()
    {
        using var e=new ElectricalTestCircuit();var r=e.Add("resistor");var floating=Solve(e);
        Assert.False(floating.Success);Assert.Contains(floating.Diagnostics,d=>d.Contains("Плавающий"));
        var b=e.Add("battery");e.Wire(b,"+",b,"-");var lcd=e.Add("lcd1602");var result=Solve(e);
        Assert.False(result.Success);Assert.Contains(result.Diagnostics,d=>d.Contains("КЗ"));Assert.Contains(lcd.Model.Id,result.UnsupportedComponents);
    }
    [Fact]
    public void SwitchPositionAndSensorsAffectStaticCircuit()
    {
        using var e=new ElectricalTestCircuit();var b=e.Add("battery");var sw=e.Add("switch");var ldr=e.Add("photoresistor");
        e.Wire(b,"+",sw,"COM");e.Wire(sw,"A",ldr,"1");e.Wire(ldr,"2",b,"-");
        ldr.Model.TrySetInput(ComponentInput.Illuminance,100);var result=Solve(e);Assert.True(result.Success);
        Assert.Equal(.009,result.Branches.Single(x=>x.ComponentId==ldr.Model.Id).Current!.Value,8);
        sw.Model.TrySetInput(ComponentInput.SwitchPosition,1);result=Solve(e);
        Assert.Equal(0,result.Branches.Single(x=>x.ComponentId==ldr.Model.Id).Current!.Value,8);
    }
    [Fact]
    public void ElectricalOverridesSurviveCopyHistoryAndFileRoundTrip()
    {
        using var e=new ElectricalTestCircuit();var led=e.Add("led");var state=new ComponentElectricalState(new Dictionary<string,double>{{"Vf",2.4},{"DynamicResistance",15}});
        Assert.True(led.Model.TrySetElectrical(state));e.Project.Refresh();var doc=e.Environment.Session.Capture();
        var path=System.IO.Path.Combine(e.Environment.Folder,"electrical.circ");
        Assert.Null(e.Environment.Repository.Save(path,doc));var loaded=e.Environment.Repository.Load(path);Assert.True(loaded.Success,loaded.Error);
        var p=Core.Documents.ProjectDocumentResolver.Resolve(loaded.Value!,e.Project.Library,e.Project.Model.Footprints,e.Environment.Mockup.Board!.Definition,new Core.Services.ComponentInsertionService());
        Assert.Equal(2.4,p.Circuit.Components[0].Electrical.Parameters["Vf"]);
        e.Project.UndoCommand.Execute(null);Assert.Empty(e.Project.Components[0].Model.Electrical.Parameters);
        e.Project.RedoCommand.Execute(null);Assert.Equal(2.4,e.Project.Components[0].Model.Electrical.Parameters["Vf"]);
    }
    [Fact]
    public void NtcTemperatureAndOverloadWarningsAreComputed()
    {
        using var e=new ElectricalTestCircuit();var battery=e.Add("battery");var ntc=e.Add("thermistor");
        e.Wire(battery,"+",ntc,"1");e.Wire(ntc,"2",battery,"-");
        var cold=Solve(e).Branches.Single(b=>b.ComponentId==ntc.Model.Id).Current!.Value;
        Assert.Equal(9d/10000,cold,8);ntc.Model.TrySetInput(ComponentInput.Temperature,50);
        Assert.True(Solve(e).Branches.Single(b=>b.ComponentId==ntc.Model.Id).Current>cold);
        var r=e.Add("resistor");r.Model.TryUpdateProperties("1 Ω",null,null,out _);e.Wire(battery,"+",r,"1");e.Wire(r,"2",battery,"-");
        var led=e.Add("led");e.Wire(battery,"+",led,"A");e.Wire(led,"K",battery,"-");
        var result=Solve(e);Assert.Contains(result.Diagnostics,d=>d.Contains("0.25"));Assert.Contains(result.Diagnostics,d=>d.Contains("выше допустимого"));
    }

    [Fact]
    public void RgbChannelsUseTheirJsonProfilesAndCommonPolarity()
    {
        using var e=new ElectricalTestCircuit();var battery=e.Add("battery");battery.Model.TrySetInput(ComponentInput.BatteryVoltage,5);var rgb=e.Add("rgb-led");
        e.Wire(battery,"+",rgb,"R");e.Wire(battery,"+",rgb,"G");e.Wire(battery,"+",rgb,"B");e.Wire(battery,"-",rgb,"COM");
        var result=Solve(e);Assert.True(result.Success,string.Join(";",result.Diagnostics));
        Assert.Equal(.3,result.Branches.Single(b=>b.Name==rgb.Reference+".R").Current!.Value,8);
        Assert.Equal(.28,result.Branches.Single(b=>b.Name==rgb.Reference+".G").Current!.Value,8);
        Assert.Equal(.2,result.Branches.Single(b=>b.Name==rgb.Reference+".B").Current!.Value,8);
        rgb.Model.TrySetInput(ComponentInput.RgbCommon,1);result=Solve(e);Assert.All(result.Branches.Where(b=>b.ComponentId==rgb.Model.Id),b=>Assert.Equal(0,b.Current));
    }

}
