using System.Globalization;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class PotentiometerEditorViewModel : ObservableObject
{
    private readonly Component model;
    private readonly Action changed;
    public PotentiometerEditorViewModel(Component model, Action changed)
    {
        this.model = model; this.changed = changed; Sync();
    }
    public double Angle => model.PotentiometerAngle;
    public double Percent => PotentiometerPosition.ToPercent(Angle);
    [ObservableProperty] private string percentText = "";
    [ObservableProperty] private string error = "";
    [RelayCommand]
    private void ApplyPercent()
    {
        if (!double.TryParse(PercentText.Replace(',', '.'), NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value) || value is < 0 or > 100)
        { Error = "Положение: число от 0 до 100 %."; return; }
        SetAngle(PotentiometerPosition.FromPercent(value));
    }
    public void SetAngle(double angle)
    {
        if (!model.TrySetPotentiometerAngle(angle)) return;
        Error = ""; Sync(); changed();
    }
    private void Sync()
    {
        PercentText = Percent.ToString("0.##", CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(Angle)); OnPropertyChanged(nameof(Percent));
    }
}
