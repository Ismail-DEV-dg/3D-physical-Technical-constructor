using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.History;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class CodeWorkspaceTests
{
    [Fact]
    public void CircuitChangesOnlyMarkCodeStaleAndEditorHistoryDoesNotEnterCircuitHistory()
    {
        using var e=new ElectricalTestCircuit();var board=e.Add("arduino-uno");var led=e.Add("led");e.Wire(board,"D9",led,"A");e.Project.Refresh();
        using var vm=new CodeViewModel(e.Project,new ArduinoCodeGenerator(new JsonCodeTemplateRepository()),new JsonCodeCompletionRepository(),new JsonCodeTemplateRepository());
        vm.GenerateCommand.Execute(null);var original=vm.Text;Assert.False(vm.IsStale);Assert.Contains("millis()",original);
        var count=e.Project.History.Count;vm.Text+="\n// manual";Assert.Equal(count,e.Project.History.Count);Assert.True(e.Project.IsDirty);
        board.MockupPlacement.MoveTo(200,300);e.Project.Refresh();Assert.False(vm.IsStale);
        var before=vm.Text;e.Add("button");Assert.True(vm.IsStale);Assert.Equal(before,vm.Text);
        Assert.Contains(vm.Completions,c=>c.Text=="LED1_PIN");Assert.Contains(vm.Completions,c=>c.Text=="pinMode");
    }
    [Fact]
    public async Task DebounceDropsOutdatedAnalysisEvenIfWorkerIgnoresCancellation()
    {
        using var e=new ElectricalTestCircuit();e.Add("arduino-uno");var service=new TestCodeAnalysisService();
        using var vm=new CodeViewModel(e.Project,analysisService:service);
        vm.Text="first";var first=vm.AnalysisTask;
        await Task.Delay(450);Assert.Single(service.Requests);
        vm.Text="second";var second=vm.AnalysisTask;
        await Task.Delay(450);Assert.Equal(2,service.Requests.Count);
        service.Requests[1].Completion.SetResult([new("latest",2)]);await second;
        service.Requests[0].Completion.SetResult([new("outdated",1)]);await first;
        Assert.Single(vm.ProblemsPanel.Items);Assert.Equal("latest",vm.ProblemsPanel.Items[0].Problem.Message);Assert.False(vm.IsAnalyzing);
        vm.Text="rapid 1";vm.Text="rapid 2";vm.Text="rapid 3";
        await Task.Delay(450);Assert.Equal(3,service.Requests.Count);Assert.Equal("rapid 3",service.Requests[2].Sketch.Text);
        service.Requests[2].Completion.SetResult([]);await vm.AnalysisTask;
    }
    [Fact]
    public async Task RealAnalysisUsesSnapshotAndTextOnlyChangesDoNotInvalidateDc()
    {
        using var e=new ElectricalTestCircuit();e.Add("esp32-devkit");var repo=new JsonElectricalLibraryRepository();
        var worker=new CodeAnalysisService(new ArduinoCodeAnalyzer(new JsonCodeCompletionRepository(),new JsonCodeTemplateRepository()),new Core.Netlist.NetlistBuilder(repo));
        using var vm=new CodeViewModel(e.Project,analysisService:worker);vm.Board=BoardKind.Esp32;
        var before=new ProjectSnapshot(e.Project.Model);var builds=e.Project.Connectivity.BuildCount;
        vm.Text="pinMode(GPIO34, OUTPUT);";await vm.AnalysisTask;
        Assert.Contains(vm.ProblemsPanel.Items,p=>p.Problem.Message.Contains("только ввод"));
        Assert.True(before.Matches(e.Project.Model));Assert.Equal(builds,e.Project.Connectivity.BuildCount);
        var target=vm.ProblemsPanel.Items[0];vm.ProblemsPanel.Selected=target;Assert.Equal(target.Problem.Line,vm.RequestedLine);
    }
    [Fact]
    public void OutputIsBoundedAndClearable()
    {
        var output=new OutputViewModel();for(var i=0;i<600;i++)output.Append("message "+i);
        Assert.Equal(500,output.Lines.Count);Assert.Contains("message 599",output.Lines[^1]);
        output.ClearCommand.Execute(null);Assert.Empty(output.Lines);
    }
}
