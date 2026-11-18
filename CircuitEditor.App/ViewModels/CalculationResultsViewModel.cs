using CircuitEditor.Core.Electronics;
using CommunityToolkit.Mvvm.ComponentModel;
namespace CircuitEditor.App.ViewModels;
public sealed partial class CalculationResultsViewModel(ProjectViewModel project) : ObservableObject
{
    public DcResult? Result {get;private set;}
    public bool IsStale {get;private set;}=true;
    public IReadOnlyList<string> Rows {get;private set;}=[];
    public string Summary=>IsStale ? "Результаты устарели — рассчитайте цепь." : Result?.Success==true ? "DC-расчёт выполнен. Напряжения относительно земли своего острова." : "Расчёт содержит ошибки; неопределённые величины не заменены нулями.";
    [ObservableProperty] private bool showVoltages;
    partial void OnShowVoltagesChanged(bool value)=>project.InvalidatePresentation();
    public void Invalidate(){Result=null;IsStale=true;Rows=[];Notify();}
    public void Set(DcResult result)
    {
        Result=result;IsStale=false;
        var nets=project.Connectivity.Netlist.Nets.Where(n=>n.Pins.Count>0 || project.Model.Circuit.Wires.Any(w=>n.Terminals.Any(t=>t.OwnerId==w.Start.OwnerId&&t.TerminalId==w.Start.TerminalId)));
        Rows=nets.Select(n=>$"{n.Name}: {QuantityFormatter.Format(result.Voltages.GetValueOrDefault(n.Id),"В")}")
            .Concat(result.Branches.Select(b=>$"{b.Name}: U={QuantityFormatter.Format(b.Voltage,"В")} · I={QuantityFormatter.Format(b.Current,"А")} · P={QuantityFormatter.Format(b.Power,"Вт")}"))
            .Concat(result.Diagnostics.Select(d=>"! "+d)).ToArray();Notify();
    }
    private void Notify(){OnPropertyChanged(nameof(Result));OnPropertyChanged(nameof(IsStale));OnPropertyChanged(nameof(Rows));OnPropertyChanged(nameof(Summary));project.InvalidatePresentation();}
}
