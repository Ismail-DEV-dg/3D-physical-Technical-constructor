using System.Collections.Immutable;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneBenchmarkDesign
{
    private const int Columns = 12;
    private const int Rows = 9;
    private const int ComponentCount = Columns * Rows;
    private const int WireCount = 200;
    private const int ActiveBranches = 4;
    private const double ColumnMm = 17;
    private const double RowMm = 12;
    private static readonly string[] Families =
        ["resistor", "resistor", "led", "capacitor", "button", "resistor", "diode", "dht11", "resistor"];

    public static CircuitProject Create(IComponentLibraryRepository library, IFootprintRepository footprints, IBreadboardRepository boards)
    {
        var definitions = library.Load().Components.ToDictionary(c => c.Id);
        var physical = footprints.Load().Value ?? throw new InvalidOperationException("Каталог корпусов недоступен.");
        var board = boards.Load().Value ?? throw new InvalidOperationException("Каталог breadboard недоступен.");
        var uno = ComponentAt(definitions["arduino-uno"], "U1", ComponentCount + 1);
        List<Component> parts = [uno];
        ViewLayout layout = new(RepresentationKind.Mockup);
        layout.Set(uno.Id, new(-62, 12));
        Dictionary<string, int> counts = [];
        for (var i = 0; i < ComponentCount; i++)
        {
            var definition = definitions[Families[(i + i / Columns) % Families.Length]];
            var count = counts.GetValueOrDefault(definition.Prefix) + 1;
            counts[definition.Prefix] = count;
            var part = ComponentAt(definition, definition.Prefix + count, i + 1);
            layout.Set(part.Id, new(i % Columns * ColumnMm + ColumnMm, i / Columns * RowMm + 16, i % 2 * 180));
            parts.Add(part);
        }

        var project = new CircuitProject(new(parts), layout, breadboards: [new BreadboardInstance(Id(500), board, 35, 123)],
            footprints: physical, simulationSetup: new()
            {
                Scenario = new("blink", ImmutableDictionary<string, PinReference>.Empty.Add("LED_PIN", new(uno.Id, "D13")),
                    ImmutableDictionary<string, double>.Empty.Add("periodMs", 800).Add("duty", .5))
            });

        var contacts = physical.ToDictionary(f => f.ComponentDefinitionId, f => f.Contacts);
        var resistors = parts.Where(p => p.Definition.Id == "resistor").Take(ActiveBranches).ToArray();
        var leds = parts.Where(p => p.Definition.Id == "led").Take(ActiveBranches).ToArray();
        var built = 0;
        for (var branch = 0; branch < ActiveBranches; branch++)
        {
            var resistor = resistors[branch];
            var led = leds[branch];
            resistor.TryUpdateProperties("1 kΩ", null, null, out _);
            Connect(uno, "D13", resistor, "lead-1");
            Connect(resistor, "lead-2", led, "anode");
            Connect(led, "cathode", uno, "GND");
        }
        var passive = parts.Skip(1).Except(resistors).Except(leds).ToArray();
        for (var i = 0; built < WireCount; i++)
        {
            var a = passive[i % passive.Length];
            var b = passive[(i + (i < passive.Length ? 1 : Columns)) % passive.Length];
            Connect(a, contacts[a.Definition.Id][0].Id, b, contacts[b.Definition.Id][^1].Id);
        }
        return project;

        void Connect(Component a, string aContact, Component b, string bContact)
        {
            var p = layout.Get(a.Id);
            var q = layout.Get(b.Id);
            project.Circuit.PutWire(new(Id(1000 + built), WireEndpoint.Contact(a.Id, aContact),
                WireEndpoint.Contact(b.Id, bContact), WirePalette.Colors[built % WirePalette.Colors.Count],
                [new LayoutPoint((p.X + q.X) / 2 + 3, (p.Y + q.Y) / 2 + 2)]));
            built++;
        }
    }

    private static Component ComponentAt(ComponentDefinition type, string reference, int id) =>
        new(type, reference, Id(id), type.DefaultValue, type.DefaultColor, type.DefaultI2cAddress);

    private static Guid Id(int id) => new(id, 9, 3, new byte[8]);
}
