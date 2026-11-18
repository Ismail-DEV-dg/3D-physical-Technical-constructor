using CircuitEditor.Core.Quantities;

namespace CircuitEditor.App.ViewModels;

public sealed record PassiveNominalOption(PassiveQuantity Quantity, decimal? Value)
{
    public string Text => Value is { } value ? PassiveNominals.Format(Quantity, value) : "Своё значение…";
    public override string ToString() => Text;
}
