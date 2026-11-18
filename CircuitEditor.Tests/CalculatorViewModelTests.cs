using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class CalculatorViewModelTests
{
    [Fact]
    public void CalculatorUsesUnitsProfilesAndBothBandDirections()
    {
        using var e=new Stage4TestEnvironment();var vm=new CalculatorViewModel(new JsonElectricalLibraryRepository(),e.Project);
        vm.OhmCommand.Execute(null);Assert.Contains("5 В",vm.Result);
        vm.DividerCommand.Execute(null);Assert.Contains("2.5 В",vm.Result);
        vm.SelectedProfile=vm.Profiles.Single(p=>p.Id=="blue");vm.LedCommand.Execute(null);Assert.Contains("3 В",vm.Result);
        vm.EncodeBandsCommand.Execute(null);vm.DecodeBandsCommand.Execute(null);Assert.Contains("220 Ω",vm.Result);
        vm.I="10 мА";vm.OhmCommand.Execute(null);Assert.Contains("ровно две",vm.Result);
        vm.U="garbage";vm.OhmCommand.Execute(null);Assert.Contains("Некорректная",vm.Result);
    }
    [Fact]
    public void SelectedLedParametersStaySelectableAndCanBeResetToAuto()
    {
        using var e=new Stage4TestEnvironment();var c=e.Project.Add(e.Project.Library.Single(d=>d.Id=="led"))!;
        var vf=c.ElectricalEditor!.Fields.Single(f=>f.Key=="Vf");vf.Text="2.4 В";c.ElectricalEditor.ApplyCommand.Execute(null);
        Assert.Equal(2.4,c.Model.Electrical.Parameters["Vf"]);
        var vm=new CalculatorViewModel(new JsonElectricalLibraryRepository(),e.Project);vm.LoadSelectedLed();
        Assert.Contains(vm.SelectedProfile!,vm.Profiles);Assert.Equal(2.4,vm.SelectedProfile!.Vf);
        vf.Text="";c.ElectricalEditor.ApplyCommand.Execute(null);Assert.Empty(c.Model.Electrical.Parameters);
    }
    [Fact]
    public void ChangingProjectInvalidatesResultsAndVoltageOverlay()
    {
        using var e=new ElectricalTestCircuit();var timer=new TestDebounceService();var repo=new JsonElectricalLibraryRepository();
        using var analysis=new ProjectAnalysisViewModel(e.Project,new Core.Validation.ErcService(repo,[]),timer,e.Environment.Navigation,new DcSolver(repo));
        var b=e.Add("battery");analysis.CalculateCommand.Execute(null);Assert.False(analysis.Results.IsStale);Assert.NotNull(analysis.Results.Result);
        analysis.Results.ShowVoltages=true;b.Model.TrySetInput(Core.Inputs.ComponentInput.BatteryVoltage,5);e.Project.Refresh();
        Assert.True(analysis.Results.IsStale);Assert.Null(analysis.Results.Result);Assert.Empty(analysis.Results.Rows);
    }
}
