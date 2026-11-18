using CircuitEditor.Core.Electronics;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ElectricalParameterViewModel(string key,string label,string quantity,double defaultValue,double? value) : ObservableObject
{
    public string Key=>key;
    public string Label=>$"{label} · авто: {defaultValue.ToString("G",CultureInfo.InvariantCulture)}";
    public string Quantity=>quantity;
    [ObservableProperty] private string text=value is null ? "" : value.Value.ToString("G",CultureInfo.InvariantCulture)+" "+(quantity switch {"U"=>"В","I"=>"А","R"=>"Ω",_=>""});
    public bool TryValue(out double number)=>quantity=="N" ? double.TryParse(Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out number) : QuantityFormatter.TryParse(Text,quantity,out number);
}
