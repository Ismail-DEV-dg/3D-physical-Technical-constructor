using CircuitEditor.App.Services;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Validation;
using CircuitEditor.Core.Physical;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ProjectAnalysisViewModel : ObservableObject, IDisposable
{
    private readonly ProjectViewModel project;
    private readonly IErcService erc;
    private readonly IDebounceService debounce;
    private readonly INavigationService navigation;
    private readonly Core.Electronics.IDcSolver? solver;
    public CalculationResultsViewModel Results {get;}
    public event EventHandler? InspectRequested;
    public void ShowResults(){InspectorTabIndex=2;InspectRequested?.Invoke(this,EventArgs.Empty);}
    [ObservableProperty] private int inspectorTabIndex;
    private IReadOnlyList<ErcIssueViewModel> issues=[];
    public IReadOnlyList<ErcIssueViewModel> AllIssues=>issues;
    public ProjectAnalysisViewModel(ProjectViewModel project,IErcService erc,IDebounceService debounce,INavigationService navigation,Core.Electronics.IDcSolver? solver=null)
    {
        this.solver=solver;Results=new CalculationResultsViewModel(project);
        this.project=project;this.erc=erc;this.debounce=debounce;this.navigation=navigation;
        project.Analysis=this;project.ModelCommitted+=Changed;Recheck();
    }
    public IReadOnlyList<string> Filters { get; }=["Все","Ошибки","Предупреждения","Информация"];
    [ObservableProperty] private string filter="Все";
    [ObservableProperty] private bool isPending;
    [ObservableProperty] private ErcIssueViewModel? selectedIssue;
    public IReadOnlyList<ErcIssueViewModel> FilteredIssues=>issues.Where(i=>Filter=="Все" || i.Issue.Severity==(Filter switch
        {"Ошибки"=>ErcSeverity.Error,"Предупреждения"=>ErcSeverity.Warning,_=>ErcSeverity.Info})).ToArray();
    public int ErrorCount=>issues.Count(i=>i.Issue.Severity==ErcSeverity.Error);
    public int WarningCount=>issues.Count(i=>i.Issue.Severity==ErcSeverity.Warning);
    public string CountSummary=>IsPending ? "ERC: ожидание…" : $"ERC: {ErrorCount} ошибок / {WarningCount} предупреждений";
    partial void OnFilterChanged(string value)=>OnPropertyChanged(nameof(FilteredIssues));
    partial void OnSelectedIssueChanged(ErcIssueViewModel? value)
    {
        project.InvalidatePresentation();
        if(value is null)return;
        if(navigation.Current.Kind is not (WorkspaceKind.Mockup or WorkspaceKind.Schematic))navigation.Navigate(WorkspaceKind.Mockup);
        var ids=value.Issue.Components.ToHashSet();foreach(var item in project.Components)item.IsSelected=ids.Contains(item.Model.Id);
        var pin=value.Issue.Pins.FirstOrDefault();var component=project.Components.FirstOrDefault(c=>c.Model.Id==(pin?.ComponentId ?? ids.FirstOrDefault()));
        if(component is null)return;
        if(navigation.Current is BreadboardViewModel mockup)
        {
            var p=component.MockupPlacement;var contact=component.Footprint.Contacts.FirstOrDefault(c=>c.ElectricalPinId==pin?.PinId);
            var point=contact is null ? new Core.Layout.LayoutPoint(p.X+component.Footprint.Body.X+component.Footprint.Body.Width/2,p.Y+component.Footprint.Body.Y+component.Footprint.Body.Height/2)
                : ContactGeometry.World(component.Footprint,contact,p,PhysicalSettings.FreeGridMm);
            mockup.OffsetX=mockup.ViewportWidth/2-PhysicalSettings.ToPixels(point.X)*mockup.Zoom;
            mockup.OffsetY=mockup.ViewportHeight/2-PhysicalSettings.ToPixels(point.Y)*mockup.Zoom;mockup.Revision++;
        }
        else if(navigation.Current is SchematicViewModel schematic)
        {
            var p=component.SchematicPlacement;var point=Core.Layout.SchematicPinGeometry.Position(component.Layout,p,pin?.PinId ?? component.Model.Definition.Pins[0].Id);
            schematic.OffsetX=schematic.ViewportWidth/2-(point?.X ?? p.X+component.Layout.Width/2)*schematic.Zoom;
            schematic.OffsetY=schematic.ViewportHeight/2-(point?.Y ?? p.Y+component.Layout.Height/2)*schematic.Zoom;schematic.Revision++;
        }
        project.InvalidatePresentation();
    }
    public ErcSeverity? Severity(PinReference pin)
    {
        var source=SelectedIssue is null ? issues : [SelectedIssue];
        return source.Where(i=>i.Issue.Pins.Contains(pin)).Select(i=>(ErcSeverity?)i.Issue.Severity).OrderBy(s=>s).FirstOrDefault();
    }
    private void Changed(object? sender,EventArgs e)
    {
        Results.Invalidate();issues=[];SelectedIssue=null;IsPending=true;Notify();debounce.Schedule(Recheck);
    }
    [RelayCommand]
    private void Calculate()
    {
        if(project.InGesture){project.StatusMessage="Завершите жест перед расчётом.";return;}
        if(solver is null)return;
        Recheck();Results.Set(solver.Solve(project.Model,project.Connectivity.Netlist));
        InspectorTabIndex=2;InspectRequested?.Invoke(this,EventArgs.Empty);
    }
    [RelayCommand]
    private void Check()
    {
        Recheck();InspectorTabIndex=1;InspectRequested?.Invoke(this,EventArgs.Empty);
    }
    private void Recheck()
    {
        if(project.InGesture){IsPending=true;debounce.Schedule(Recheck);return;}
        issues=erc.Check(project.Model,project.Connectivity.Netlist).Select(i=>new ErcIssueViewModel(i)).ToArray();IsPending=false;Notify();
    }
    private void Notify()
    {
        OnPropertyChanged(nameof(FilteredIssues));OnPropertyChanged(nameof(ErrorCount));OnPropertyChanged(nameof(WarningCount));OnPropertyChanged(nameof(CountSummary));
        project.InvalidatePresentation();
    }
    public void Dispose(){project.ModelCommitted-=Changed;debounce.Dispose();}
}
