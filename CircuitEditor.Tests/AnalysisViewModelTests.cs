using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Validation;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class AnalysisViewModelTests
{
    [Fact]
    public void DebounceFiltersAndFocusUseCommittedModel()
    {
        using var e=new ElectricalTestCircuit();var timer=new TestDebounceService();
        using var vm=new ProjectAnalysisViewModel(e.Project,new ErcService(new JsonElectricalLibraryRepository(),[new SupplyShortRule(),new DisconnectedSupplyRule()]),timer,e.Environment.Navigation);
        var uno=e.Add("arduino-uno");e.Wire(uno,"5V",uno,"GND");e.Project.Refresh();
        Assert.True(vm.IsPending);Assert.NotNull(timer.Pending);timer.Flush();Assert.Equal(1,vm.ErrorCount);
        e.Add("servo");timer.Flush();Assert.True(vm.WarningCount>0);vm.Filter="Ошибки";Assert.Single(vm.FilteredIssues);
        var old=e.Environment.Mockup.OffsetX;vm.SelectedIssue=vm.FilteredIssues[0];Assert.True(uno.IsSelected);Assert.NotEqual(old,e.Environment.Mockup.OffsetX);
        e.Project.BeginGesture();e.Project.Components[0].SchematicPlacement.MoveTo(400,300);vm.CheckCommand.Execute(null);Assert.True(vm.IsPending);
        e.Project.EndGesture();timer.Flush();Assert.False(vm.IsPending);
    }
}
