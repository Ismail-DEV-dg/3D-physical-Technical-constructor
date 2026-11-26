using System.Windows.Threading;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.History;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class CodeWorkspaceTests
{
    [Fact]
    public void CircuitChangesPreserveCodeAndEditorHistoryDoesNotEnterCircuitHistory()
    {
        using var e=new ElectricalTestCircuit();var board=e.Add("arduino-uno");var led=e.Add("led");e.Wire(board,"D9",led,"A");e.Project.Refresh();
        using var vm=new CodeViewModel(e.Project,new JsonCodeCompletionRepository(),new JsonCodeTemplateRepository());
        Assert.Equal(SketchDocument.StarterText, vm.Text);
        var count=e.Project.History.Count;vm.Text+="\n// manual";Assert.Equal(count,e.Project.History.Count);Assert.True(e.Project.IsDirty);
        board.MockupPlacement.MoveTo(200,300);e.Project.Refresh();
        var before=vm.Text;e.Add("button");Assert.Equal(before,vm.Text);
        Assert.Contains(vm.Completions,c=>c.Text=="LED1_PIN");Assert.Contains(vm.Completions,c=>c.Text=="pinMode");
    }
    [Fact]
    public void DebounceDropsOutdatedAnalysisEvenIfWorkerIgnoresCancellation() =>
        StaRunner.Run(RunDebounceScenarioOnDispatcher);

    private static void RunDebounceScenarioOnDispatcher()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var scenario = dispatcher.InvokeAsync(VerifyDebounceAsync).Task.Unwrap();
        _ = scenario.ContinueWith(_ => dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal), TaskScheduler.Default);
        Dispatcher.Run();
        scenario.GetAwaiter().GetResult();
    }

    private static async Task VerifyDebounceAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var design = new ElectricalTestCircuit();
        design.Add("arduino-uno");
        var service = new TestCodeAnalysisService();
        using var vm = new CodeViewModel(design.Project, analysisService: service);

        vm.Text = "first";
        var firstTask = vm.AnalysisTask;
        var first = await service.NextRequestAsync(timeout.Token);
        Assert.Equal("first", first.Sketch.Text);

        vm.Text = "second";
        var secondTask = vm.AnalysisTask;
        var second = await service.NextRequestAsync(timeout.Token);
        Assert.Equal("second", second.Sketch.Text);

        second.Completion.SetResult([new("latest", 2)]);
        await secondTask.WaitAsync(timeout.Token);
        first.Completion.SetResult([new("outdated", 1)]);
        await firstTask.WaitAsync(timeout.Token);
        Assert.Equal("latest", Assert.Single(vm.ProblemsPanel.Items).Problem.Message);
        Assert.False(vm.IsAnalyzing);

        // All edits run in one dispatcher turn, so analysis cannot begin between them.
        vm.Text = "rapid 1";
        var skippedFirst = vm.AnalysisTask;
        vm.Text = "rapid 2";
        var skippedSecond = vm.AnalysisTask;
        vm.Text = "rapid 3";
        var latestTask = vm.AnalysisTask;

        var latest = await service.NextRequestAsync(timeout.Token);
        Assert.Equal("rapid 3", latest.Sketch.Text);
        latest.Completion.SetResult([]);
        await Task.WhenAll(skippedFirst, skippedSecond, latestTask).WaitAsync(timeout.Token);
        Assert.False(service.HasPendingRequests);
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
