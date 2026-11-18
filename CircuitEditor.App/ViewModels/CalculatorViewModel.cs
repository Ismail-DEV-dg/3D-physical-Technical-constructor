using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Resistance;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
namespace CircuitEditor.App.ViewModels;
public sealed partial class CalculatorViewModel : ObservableObject
{
    private readonly ProjectViewModel project;
    private readonly ElectricalCatalog? catalog;
    public CalculatorViewModel(IElectricalLibraryRepository repository,ProjectViewModel project)
    {
        this.project=project;var data=repository.Load();catalog=data.Value;Profiles=new ObservableCollection<LedProfile>(catalog?.LedProfiles??[]);
        Bands.CollectionChanged+=(_,_)=>OnPropertyChanged(nameof(VisibleBands));
        selectedProfile=Profiles.FirstOrDefault();Result=data.Error??"Введите значения с единицами; для закона Ома заполните ровно два поля.";
    }
    public event EventHandler? CloseRequested;
    [RelayCommand] private void Close()=>CloseRequested?.Invoke(this,EventArgs.Empty);
    public ObservableCollection<LedProfile> Profiles {get;}
    public IReadOnlyList<ResistorSeriesKind> SeriesOptions {get;}=Enum.GetValues<ResistorSeriesKind>();
    public IReadOnlyList<int> BandCounts {get;}=[4,5];
    public IReadOnlyList<string> Colors=>ResistorMarking.Colors;
    public ObservableCollection<string> Bands {get;}=new(ResistorMarking.GetBands("220 Ω",5,4).Append(ResistorMarking.Colors[^2]));
    [ObservableProperty] private string u="5 В";
    [ObservableProperty] private string i="";
    [ObservableProperty] private string r="1 kΩ";
    [ObservableProperty] private string p="";
    [ObservableProperty] private string dividerSupply="5 В";
    [ObservableProperty] private string upper="1 kΩ";
    [ObservableProperty] private string lower="1 kΩ";
    [ObservableProperty] private string ledSupply="5 В";
    [ObservableProperty] private string ledCurrent="15 мА";
    [ObservableProperty] private LedProfile? selectedProfile;
    [ObservableProperty] private ResistorSeriesKind series=ResistorSeriesKind.E12;
    [ObservableProperty] private string bandResistance="220 Ω";
    [ObservableProperty] private int bandCount=4;
    [ObservableProperty] private string result="";
    public IReadOnlyList<string> VisibleBands=>Bands.Take(BandCount).ToArray();
    partial void OnBandCountChanged(int value)=>OnPropertyChanged(nameof(VisibleBands));
    public void LoadSelectedLed()
    {
        if(project.SelectedComponent?.Model is { } c && c.Definition.Id=="led" && catalog is not null)
        {
            foreach(var old in Profiles.Where(p=>p.Id=="selected-instance").ToArray())Profiles.Remove(old);
            var profile=ElectricalParameters.Led(c,catalog) with {Id="selected-instance",Name="Выбранный "+c.Reference};
            Profiles.Insert(0,profile);SelectedProfile=profile;
        }
    }
    [RelayCommand]
    private void Ohm()
    {
        var fields=new[]{(U,"U"),(I,"I"),(R,"R"),(P,"P")};var values=new double?[4];
        for(var n=0;n<fields.Length;n++)
            if(!string.IsNullOrWhiteSpace(fields[n].Item1))
            {if(!QuantityFormatter.TryParse(fields[n].Item1,fields[n].Item2,out var value)){Result="Некорректная величина "+fields[n].Item2+". Укажите единицу.";return;}values[n]=value;}
        var answer=OhmCalculator.Solve(values[0],values[1],values[2],values[3]);
        Result=answer.Error??$"U = {QuantityFormatter.Format(answer.Value!.Voltage,"В")} · I = {QuantityFormatter.Format(answer.Value.Current,"А")} · R = {QuantityFormatter.Resistance(answer.Value.Resistance)} · P = {QuantityFormatter.Format(answer.Value.Power,"Вт")}";
    }
    [RelayCommand]
    private void Divider()
    {
        if(!QuantityFormatter.TryParse(DividerSupply,"U",out var v)||!QuantityFormatter.TryParse(Upper,"R",out var a)||!QuantityFormatter.TryParse(Lower,"R",out var b))
        {Result="Введите питание и два сопротивления с единицами.";return;}
        Result=VoltageDividerCalculator.Calculate(v,a,b) is { } value ? "Uout = "+QuantityFormatter.Format(value,"В") : "Некорректные значения делителя.";
    }
    [RelayCommand]
    private void Led()
    {
        if(SelectedProfile is null || !QuantityFormatter.TryParse(LedSupply,"U",out var v)||!QuantityFormatter.TryParse(LedCurrent,"I",out var current))
        {Result="Выберите профиль LED и укажите питание/ток с единицами.";return;}
        var answer=LedResistorCalculator.Calculate(v,current,SelectedProfile,Series);
        Result=answer.Error??$"Vf = {QuantityFormatter.Format(answer.Value!.Vf,"В")} · R ≥ {QuantityFormatter.Resistance(answer.Value.Resistance)} · I = {QuantityFormatter.Format(answer.Value.Current,"А")} · P(R) = {QuantityFormatter.Format(answer.Value.Power,"Вт")}";
        if(answer.Value?.Current>SelectedProfile.MaximumCurrent)Result+=" · Внимание: ток выше допустимого для профиля LED.";
    }
    [RelayCommand]
    private void EncodeBands()
    {
        var bands=ResistorMarking.GetBands(BandResistance,5,BandCount);
        if(bands.Count==0){Result="Этот номинал не представим выбранным числом полос.";return;}
        for(var n=0;n<bands.Count;n++)Bands[n]=bands[n];Result="Полосы рассчитаны; допуск 5%.";
    }
    [RelayCommand]
    private void DecodeBands()
    {
        var answer=ResistorMarking.Decode(Bands.Take(BandCount).ToArray());
        Result=answer.Error??$"{ResistanceFormatter.Format(answer.Value!.Ohms)} ±{answer.Value.TolerancePercent:G}%";
    }
}
