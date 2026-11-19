namespace CircuitEditor.App.ViewModels;

public sealed class ChipTruthRowEdit(IReadOnlyList<string> row)
{
    public string Inputs { get; } = row[0];
    public string Outputs { get; set; } = row[1];
}
