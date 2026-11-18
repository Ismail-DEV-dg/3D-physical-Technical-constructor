using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ElectricalPropertiesViewModel : ObservableObject
{
    private readonly Component component;
    private readonly Action changed;
    public ElectricalPropertiesViewModel(Component component,ElectricalCatalog catalog,Action changed)
    {
        this.component=component;this.changed=changed;var definition=catalog.Find(component.Definition.Id);var fields=new List<ElectricalParameterViewModel>();
        void Add(string key,string label,string quantity,double fallback)=>fields.Add(new(key,label,quantity,fallback,component.Electrical.Parameters.TryGetValue(key,out var v)?v:null));
        if(component.Definition.Id=="led")
        {var p=catalog.Led(component.Color);Add("Vf","Vf, В","U",p.Vf);Add("DynamicResistance","Динамическое R, Ω","R",p.DynamicResistance);Add("MaximumCurrent","Максимальный I, А","I",p.MaximumCurrent);}
        if(component.Definition.Id=="rgb-led")
        {
            foreach(var channel in definition!.ChannelProfiles!.Keys)
            {var profile=catalog.LedProfiles.Single(p=>p.Id==definition.ChannelProfiles[channel]);Add(channel+".Vf",channel+" Vf, В","U",profile.Vf);}
            var first=catalog.LedProfiles.Single(p=>p.Id==definition.ChannelProfiles.Values.First());
            Add("DynamicResistance","Динамическое R канала, Ω","R",first.DynamicResistance);Add("MaximumCurrent","Макс. I канала, А","I",first.MaximumCurrent);
        }
        if(component.Definition.Id=="thermistor"){Add("R25","R при 25 °C, Ω","R",definition!.R25!.Value);Add("Beta","Beta, K","N",definition.Beta!.Value);}
        if(component.Definition.Id=="photoresistor"){Add("LightResistance","R при 100%, Ω","R",definition!.LightResistance!.Value);Add("DarkResistance","R при 0%, Ω","R",definition.DarkResistance!.Value);}
        Fields=fields;
    }
    public IReadOnlyList<ElectricalParameterViewModel> Fields {get;}
    [ObservableProperty] private string error="";
    [RelayCommand]
    private void Apply()
    {
        var values=new Dictionary<string,double>();
        foreach(var field in Fields.Where(f=>!string.IsNullOrWhiteSpace(f.Text)))
        {if(!field.TryValue(out var v)||!double.IsFinite(v)||v<=0){Error="Положительное число с единицей: "+field.Key;return;}values[field.Key]=v;}
        if(!component.TrySetElectrical(new(values,component.Electrical.PinRoles))){Error="Параметры вне допустимой модели.";return;}
        Error="";changed();
    }
}
