using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Core.Electronics;

namespace CircuitEditor.App.Controls;

internal static class PhysicalPinToolTips
{
    public static string? Find(BreadboardViewModel scene, double x, double y) => Find(scene, x, y, out _);

    public static string? Find(BreadboardViewModel scene, double x, double y, out bool splitter)
    {
        splitter = false;
        const double contactHitRadiusMm = .9;
        var pitch = scene.Board?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm;
        foreach (var item in scene.Project.Components.Reverse())
        {
            if (!scene.Hit(item, x, y)) continue;
            foreach (var contact in item.Footprint.Contacts)
            {
                var position = ContactGeometry.World(item.Footprint, contact, scene.PlacementOf(item), pitch);
                if (Math.Pow(position.X - x, 2) + Math.Pow(position.Y - y, 2) > contactHitRadiusMm * contactHitRadiusMm) continue;
                var pin = item.Model.Definition.Pins.Single(pin => pin.Id == contact.ElectricalPinId);
                if (item.Model.SplitterOutputs is not null)
                {
                    splitter = true;
                    return SplitterVoltage(scene, item, contact, pin.Id);
                }
                var legend = pin.Id is "RESET" or "EN" ? "\nСброс / разрешение: active-low, низкий уровень удерживает сброс."
                    : pin.IsInputOnly ? "\ninput only: только вход." : "";
                if (pin.IsNoConnect) legend += "\nNC: ножка присутствует, внутреннего электрического соединения нет.";
                if (item.Model.Definition.Id == "rgb-led" && pin.Id == "COM")
                    legend += "\n" + ComponentInputDefinitions.For("rgb-led")[0].Format(item.Model.Inputs.Values[ComponentInput.RgbCommon]);
                return $"{item.Reference} · контакт {contact.Id}\n{pin.Name} · {pin.Type}{legend}";
            }
            return null;
        }
        return null;
    }

    private static string SplitterVoltage(BreadboardViewModel scene, ComponentViewModel item, ContactDefinition contact, string pinId)
    {
        bool Connected(WireEndpoint endpoint) => endpoint.Kind == WireEndpointKind.ComponentContact && endpoint.OwnerId == item.Model.Id;
        var wires = scene.Project.Model.Circuit.Wires.Where(wire => Connected(wire.Start) || Connected(wire.End)).ToArray();
        double? voltage = null;
        if (wires.Length != 0)
        {
            voltage = scene.Simulation is { HasSession: true } simulation
                ? wires.Select(wire => simulation.State.Wires.GetValueOrDefault(wire.Id)?.Voltage).FirstOrDefault(value => value.HasValue)
                : scene.Project.Analysis?.ReadStaticVoltage(new PinReference(item.Model.Id, pinId));
        }
        var value = voltage is { } measured ? QuantityFormatter.Format(measured, "В") : "напряжение не определено";
        return $"{item.Reference} · {contact.Id}\n{value} · общая шина";
    }
}
