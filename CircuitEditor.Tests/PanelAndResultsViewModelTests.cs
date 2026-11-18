using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Validation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class PanelAndResultsViewModelTests
{
    [Fact]
    public void UsedResultsExcludeFreeBoardPinsAndDisconnectedParts()
    {
        using var e = new ElectricalTestCircuit();
        var uno = e.Add("arduino-uno"); var r = e.Add("resistor"); var unused = e.Add("resistor");
        e.Wire(uno,"5V",r,"1");e.Wire(r,"2",uno,"GND");e.Project.Refresh();
        var vm = new CalculationResultsViewModel(e.Project);
        var result = new DcSolver(new JsonElectricalLibraryRepository()).Solve(e.Project.Model,e.Project.Connectivity.Netlist);
        vm.Set(result);
        var usedNodes = vm.Entries.Where(x=>x.Category=="Узел").ToArray();
        Assert.Equal(2,usedNodes.Length);
        Assert.DoesNotContain(usedNodes,x=>x.Detail.Contains(".3V3") || x.Detail.Contains(unused.Reference+"."));
        Assert.Contains(vm.Entries,x=>x.Category=="Деталь" && x.Title==r.Reference);
        Assert.DoesNotContain(vm.Entries,x=>x.Category=="Деталь" && x.Title==unused.Reference);
        Assert.DoesNotContain(vm.Entries,x=>x.Category=="Деталь" && x.Title.EndsWith(".3V3"));
        vm.ShowEverythingCommand.Execute(null);
        Assert.True(vm.ShowAll); Assert.False(vm.UsedOnly);
        Assert.Contains(vm.Entries,x=>x.Category=="Узел" && x.Detail.Contains(".3V3"));
        Assert.Contains(vm.Entries,x=>x.Category=="Узел" && x.Detail.Contains(unused.Reference+"."));
        Assert.Contains(vm.Entries,x=>x.Category=="Деталь" && x.Title.EndsWith(".3V3"));
        vm.ShowUsedCommand.Execute(null);Assert.Equal(usedNodes,vm.Entries.Where(x=>x.Category=="Узел"));
        Assert.Same(result,vm.Result);
    }
    [Fact]
    public void EmptyBreadboardGroupsAreExcludedButWiredHolesAreUsed()
    {
        using var e=new Stage4TestEnvironment();var board=e.Mockup.Board!;
        var vm=new CalculationResultsViewModel(e.Project);
        var empty=new DcResult(true,new Dictionary<string,double?>(),[],[],[]);
        vm.Set(empty);Assert.Empty(vm.Entries);
        var holes=board.Definition.Holes.GroupBy(h=>h.GroupId).Take(2).Select(g=>g.First()).ToArray();
        e.Project.Model.Circuit.PutWire(new Wire(WireEndpoint.Hole(board.Id,holes[0].Id),WireEndpoint.Hole(board.Id,holes[1].Id)));
        e.Project.Refresh();vm.Set(empty);
        Assert.Single(vm.Entries);Assert.Contains("не определено",vm.Entries[0].Value);
        vm.ShowEverythingCommand.Execute(null);Assert.Single(vm.Entries);
        vm.Invalidate();Assert.Empty(vm.Entries);Assert.True(vm.IsStale);Assert.False(vm.HasEntries);
    }
    [Fact]
    public void CalculationAndManualCheckRestoreInspectorWithoutAutomaticErcStealingFocus()
    {
        using var e=new ElectricalTestCircuit();var repo=new JsonElectricalLibraryRepository();var timer=new TestDebounceService();
        using var analysis=new ProjectAnalysisViewModel(e.Project,new ErcService(repo,[]),timer,e.Environment.Navigation,new DcSolver(repo));
        using var main=new MainViewModel(e.Environment.Navigation,new ThemeService(new TestThemeResources()),new TestWindowService(),analysis:analysis);
        main.Panels.Inspector.HideCommand.Execute(null);
        e.Add("resistor");timer.Flush();Assert.False(main.Panels.Inspector.IsVisible);
        analysis.CalculateCommand.Execute(null);Assert.True(main.Panels.Inspector.IsVisible);Assert.Equal(2,analysis.InspectorTabIndex);
        main.Panels.Inspector.HideCommand.Execute(null);
        analysis.CheckCommand.Execute(null);Assert.True(main.Panels.Inspector.IsExpanded);Assert.Equal(1,analysis.InspectorTabIndex);
    }
    [Fact]
    public void PinHideRestoreAndResetPreserveIndependentPanelSizes()
    {
        var layout=new PanelLayoutViewModel();var pane=layout.Components;
        pane.Size=280;pane.TogglePinCommand.Execute(null);
        Assert.False(pane.IsPinned);Assert.False(pane.IsExpanded);
        pane.ToggleOpenCommand.Execute(null);Assert.True(pane.IsExpanded);
        pane.CollapseIfUnpinned();Assert.False(pane.IsExpanded);
        pane.ShowCommand.Execute(null);pane.TogglePinCommand.Execute(null);pane.CollapseIfUnpinned();Assert.True(pane.IsExpanded);
        pane.HideCommand.Execute(null);Assert.False(pane.IsVisible);
        pane.ShowCommand.Execute(null);Assert.True(pane.IsExpanded);Assert.Equal(280,pane.Size);
        layout.Inspector.Size=410;layout.Analysis.Size=230;layout.Analysis.HideCommand.Execute(null);
        layout.ResetCommand.Execute(null);
        Assert.Equal(224,pane.Size);Assert.Equal(310,layout.Inspector.Size);Assert.Equal(140,layout.Analysis.Size);
        Assert.True(layout.Analysis.IsVisible);Assert.True(layout.Analysis.IsPinned);
    }
}
