using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Chips;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class ChipEditorTests
{
    [Fact]
    public void AppliesPinsAndKeepsEditorHistoryIndependent()
    {
        var model = new ChipViewModel(new JsonLogicFamilyRepository());
        Assert.Empty(model.Problems);
        model.SelectedPackage = "DIP-14";
        model.ChipName = "Тест";
        model.ApplyPinsCommand.Execute(null);
        Assert.Equal(14, model.Pins.Count);
        Assert.Equal("Тест", model.Definition.Name);
        model.UndoCommand.Execute(null);
        Assert.Equal(8, model.Pins.Count);
        model.RedoCommand.Execute(null);
        Assert.Equal(14, model.Pins.Count);
        model.Pins.Single(pin => pin.Number == 2).Name = "A";
        model.ApplyPinsCommand.Execute(null);
        Assert.Contains(model.Problems, error => error.Contains("уникальное"));
    }

    [Fact]
    public void GeneratesSortedColumnsEditsCellsAndRoundTripsCsv()
    {
        var model = new ChipViewModel(new JsonLogicFamilyRepository());
        model.Truth.InputPins = "2, 1";
        model.Truth.GenerateCommand.Execute(null);
        Assert.Equal("1, 2", model.Truth.InputPins);
        Assert.Equal(4, model.Truth.Rows.Count);
        model.Truth.Rows[0].Outputs = "0";
        model.Truth.ApplyCellsCommand.Execute(null);
        var csv = ChipTruthCsv.Format(Assert.IsType<ChipTruthTable>(model.Definition.Behavior));
        Assert.StartsWith("IN:1;IN:2;OUT:3", csv);
        Assert.Equal(csv, ChipTruthCsv.Format(ChipTruthCsv.Parse(csv)));
        Assert.Throws<FormatException>(() => ChipTruthCsv.Parse("IN:1;OUT:3\n0;?"));
    }

    [Fact]
    public void WiresGraphAndUndoRestoresDisconnectedState()
    {
        var model = new ChipViewModel(new JsonLogicFamilyRepository());
        model.ShowGraphCommand.Execute(null);
        model.Graph.GateKind = ChipGateKind.Nand;
        model.Graph.GatePorts = "P1, P2";
        model.Graph.AddGateCommand.Execute(null);
        model.Graph.OutputPin = "3";
        model.Graph.OutputSource = "B1";
        model.Graph.ConnectCommand.Execute(null);
        Assert.Empty(model.Problems);
        model.UndoCommand.Execute(null);
        Assert.Contains(model.Problems, error => error.Contains("драйвер"));
        model.RedoCommand.Execute(null);
        Assert.Empty(model.Problems);
    }
}
