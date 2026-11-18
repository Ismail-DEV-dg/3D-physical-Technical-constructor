using CircuitEditor.Core.Electronics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class CalculationResultsViewModel(ProjectViewModel project) : ObservableObject
{
    public DcResult? Result {get;private set;}
    public bool IsStale {get;private set;}=true;
    public IReadOnlyList<CalculationRowViewModel> Entries {get;private set;}=[];
    public IReadOnlyList<string> Rows => Entries.Select(e=>$"{e.Title}: {e.Value} {e.Detail}").ToArray();
    public string Summary=>IsStale ? "Результаты устарели — рассчитайте цепь." : Result?.Success==true ? "DC-расчёт выполнен. Напряжения относительно земли своего острова." : "Расчёт содержит ошибки; неопределённые величины не заменены нулями.";
    public string EmptyMessage => IsStale ? "Нажмите «Рассчитать цепь» внизу или F6." : "Нет подключённых узлов. Соедините детали или выберите «Весь список».";
    public bool HasEntries => Entries.Count > 0;
    public bool UsedOnly => !ShowAll;
    [ObservableProperty] private bool showAll;
    [ObservableProperty] private bool showVoltages;
    partial void OnShowAllChanged(bool value) { OnPropertyChanged(nameof(UsedOnly)); UpdateEntries(); }
    partial void OnShowVoltagesChanged(bool value)=>project.InvalidatePresentation();
    [RelayCommand] private void ShowUsed() => ShowAll=false;
    [RelayCommand] private void ShowEverything() => ShowAll=true;
    public void Invalidate(){Result=null;IsStale=true;UpdateEntries();}
    public void Set(DcResult result) { Result=result;IsStale=false;UpdateEntries(); }
    private void UpdateEntries()
    {
        if (Result is not { } result) Entries=[];
        else
        {
            var endpoints=project.Model.Circuit.Wires.SelectMany(w=>new[]{w.Start,w.End})
                .Select(e=>(Kind:e.Kind==Core.Wiring.WireEndpointKind.ComponentContact?"c":"h",e.OwnerId,e.TerminalId)).ToHashSet();
            var allNets=project.Connectivity.Netlist.Nets;
            // Internal board links and unused breadboard rows do not mean an external connection.
            var used=allNets.Where(n=>n.Pins.Select(p=>p.ComponentId).Distinct().Skip(1).Any()
                || n.Terminals.Any(t=>endpoints.Contains((t.Kind,t.OwnerId,t.TerminalId)))).ToArray();
            var usedNetIds=used.Select(n=>n.Id).ToHashSet();
            var nets=ShowAll ? allNets.Where(n=>n.Pins.Count>0 || used.Contains(n)) : used;
            var components=project.Model.Circuit.Components.ToDictionary(c=>c.Id);
            Entries=nets.Select(n=>new CalculationRowViewModel(n.Name,
                    QuantityFormatter.Format(result.Voltages.GetValueOrDefault(n.Id),"В"),
                    string.Join(" · ",n.Pins.Select(p=>$"{components[p.ComponentId].Reference}.{p.PinId}")),"Узел"))
                .Concat(result.Branches.Where(b=>ShowAll || usedNetIds.Contains(b.A) && usedNetIds.Contains(b.B)).Select(b=>
                    new CalculationRowViewModel(b.Name,QuantityFormatter.Format(b.Voltage,"В"),
                        $"I = {QuantityFormatter.Format(b.Current,"А")}   P = {QuantityFormatter.Format(b.Power,"Вт")}","Деталь")))
                .Concat(result.Diagnostics.Select(d=>new CalculationRowViewModel("Диагностика","",d,"Сообщение"))).ToArray();
        }
        OnPropertyChanged(nameof(Result));OnPropertyChanged(nameof(IsStale));OnPropertyChanged(nameof(Entries));
        OnPropertyChanged(nameof(Rows));OnPropertyChanged(nameof(Summary));OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(EmptyMessage));project.InvalidatePresentation();
    }
}
