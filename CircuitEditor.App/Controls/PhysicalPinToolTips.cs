using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Inputs;

namespace CircuitEditor.App.Controls;

internal static class PhysicalPinToolTips
{
    public static string? Find(BreadboardViewModel scene, double x, double y)
    {
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
}
