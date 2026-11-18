namespace CircuitEditor.App.ViewModels;

public sealed record InputChoiceViewModel(double Value, string Text)
{
    public override string ToString() => Text;
}
