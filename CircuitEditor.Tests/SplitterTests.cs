using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Controls;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SplitterTests
{
    [Fact]
    public void HoveringIndividualSocketsShowsTheCalculatedBusVoltage()
    {
        using var design = new ElectricalTestCircuit();
        var catalog = new JsonElectricalLibraryRepository();
        using var analysis = new ProjectAnalysisViewModel(design.Project,
            new Core.Validation.ErcService(catalog, []), new TestDebounceService(), design.Environment.Navigation, new DcSolver(catalog));
        var battery = design.Add("battery");
        var power = design.Add("splitter");
        var ground = design.Add("splitter");
        Assert.True(battery.Model.TrySetInput(ComponentInput.BatteryVoltage, 5));
        design.Wire(battery, "+", power, "BUS");
        design.Wire(battery, "-", ground, "BUS");
        design.Project.Refresh();
        var mockup = design.Environment.Mockup;
        foreach (var (component, expected) in new[] { (power, "5 В"), (ground, "0 В") })
            foreach (var port in component.Footprint.Contacts)
            {
                var p = Core.Physical.ContactGeometry.World(component.Footprint, port, component.MockupPlacement,
                    Core.Physical.PhysicalSettings.FreeGridMm);
                var tooltip = PhysicalPinToolTips.Find(mockup, p.X, p.Y);
                Assert.Contains(port.Id, tooltip);
                Assert.Contains(expected, tooltip);
            }
    }

    [Fact]
    public void LoadsShareVoltageAndDrawDifferentCurrents()
    {
        using var design = new ElectricalTestCircuit();
        var supply = design.Add("battery");
        Assert.True(supply.Model.TrySetInput(ComponentInput.BatteryVoltage, 5));
        var power = design.Add("splitter");
        var ground = design.Add("splitter");
        var first = design.Add("resistor");
        var second = design.Add("resistor");
        Assert.True(first.Model.TryUpdateProperties("1 kΩ", null, null, out _));
        Assert.True(second.Model.TryUpdateProperties("2 kΩ", null, null, out _));
        design.Wire(supply, "+", power, "BUS");
        design.Wire(supply, "-", ground, "BUS");
        Connect(design, power, "OUT1", first, "lead-1");
        Connect(design, power, "OUT2", second, "lead-1");
        Connect(design, ground, "OUT1", first, "lead-2");
        Connect(design, ground, "OUT2", second, "lead-2");

        var nets = design.Nets();
        var result = new DcSolver(new JsonElectricalLibraryRepository()).Solve(design.Project.Model, nets);
        Assert.True(result.Success, string.Join(", ", result.Diagnostics));
        Assert.DoesNotContain(power.Model.Id, result.UnsupportedComponents);
        Assert.Equal(5, result.Voltages[nets.Find(power.Model.Id, "BUS")!.Id]);
        Assert.Equal(0, result.Voltages[nets.Find(ground.Model.Id, "BUS")!.Id]);
        Assert.Equal(.005, result.Branches.Single(branch => branch.ComponentId == first.Model.Id).Current!.Value, 9);
        Assert.Equal(.0025, result.Branches.Single(branch => branch.ComponentId == second.Model.Id).Current!.Value, 9);

        var sourceResistance = design.Add("resistor");
        Assert.True(sourceResistance.Model.TryUpdateProperties("100 Ω", null, null, out _));
        var direct = design.Project.Model.Circuit.Wires.Single(wire => wire.Start.OwnerId == supply.Model.Id && wire.End.OwnerId == power.Model.Id);
        design.Project.Model.Circuit.RemoveWire(direct.Id);
        design.Wire(supply, "+", sourceResistance, "1");
        Connect(design, sourceResistance, "lead-2", power, "IN");
        nets = design.Nets();
        result = new DcSolver(new JsonElectricalLibraryRepository()).Solve(design.Project.Model, nets);
        Assert.True(result.Success);
        Assert.Equal(5d / (1 + 100 * (1d / 1000 + 1d / 2000)),
            result.Voltages[nets.Find(power.Model.Id, "BUS")!.Id]!.Value, 8);
    }

    [Fact]
    public void ToolbarResizesAndStoresTheExactSocketsWithoutDisconnectingWires()
    {
        using var design = new ElectricalTestCircuit();
        var splitter = design.Add("splitter");
        var load = design.Add("led");
        design.Project.Select(splitter);
        var mockup = design.Environment.Mockup;
        Assert.Equal("Выходы", mockup.SpanLabel);
        Assert.Equal(3, mockup.SelectedSpan);
        Assert.Equal(4, splitter.Footprint.Contacts.Count);
        mockup.SelectedSpan = 5;
        Assert.Equal(6, splitter.Footprint.Contacts.Count);
        Connect(design, splitter, "OUT5", load, "anode");
        design.Project.Refresh();
        mockup.SelectedSpan = 3;
        Assert.Equal(5, mockup.SelectedSpan);
        Assert.Single(design.Project.Model.Circuit.Wires);
        Assert.Contains("отсоедините", design.Project.StatusMessage);

        var path = System.IO.Path.Combine(design.Environment.Folder, "splitter.circ");
        Assert.Null(design.Environment.Repository.Save(path, design.Environment.Session.Capture()));
        Assert.True(design.Environment.Session.Load(path));
        var reopened = design.Project.Components.Single(item => item.Model.Id == splitter.Model.Id);
        Assert.Equal(5, reopened.Model.SplitterOutputs);
        Assert.Equal(6, reopened.Footprint.Contacts.Count);
        var scene = new SceneBuilder(new JsonSceneCatalogRepository()).Build(design.Project.Model);
        Assert.Equal(6, scene.Nodes.Single(node => node.Id == reopened.Model.Id).Contacts.Length);
        design.Project.Select(reopened);
        mockup.SelectedSpan = 8;
        design.Project.UndoCommand.Execute(null);
        Assert.Equal(5, mockup.SelectedSpan);
        design.Project.RedoCommand.Execute(null);
        Assert.Equal(8, mockup.SelectedSpan);
    }

    private static void Connect(ElectricalTestCircuit design, ComponentViewModel a, string pinA, ComponentViewModel b, string pinB)
    {
        var wire = new Wire(WireEndpoint.Contact(a.Model.Id, pinA), WireEndpoint.Contact(b.Model.Id, pinB));
        Assert.Null(WireGeometry.Validate(design.Project.Model, wire));
        design.Project.Model.Circuit.PutWire(wire);
    }
}
