using CircuitEditor.Core.Resistance;

namespace CircuitEditor.App.ViewModels;

public sealed record ResistanceOption(decimal? Ohms)
{
    public string Text => Ohms is { } value ? ResistanceFormatter.Format(value) : "Своё значение…";
    public override string ToString() => Text;
}
