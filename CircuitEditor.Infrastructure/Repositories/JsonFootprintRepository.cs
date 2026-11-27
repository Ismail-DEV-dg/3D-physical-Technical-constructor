using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Infrastructure.Data;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonFootprintRepository : IFootprintRepository
{
    public RepositoryResult<IReadOnlyList<FootprintDefinition>> Load()
    {
        try { using var stream = EmbeddedJson.Open("footprints.json"); return Read(stream); }
        catch (Exception error) when (EmbeddedJson.IsDataError(error)) { return new(null, error.Message); }
    }
    public static RepositoryResult<IReadOnlyList<FootprintDefinition>> Read(Stream stream)
    {
        try
        {
            var data = EmbeddedJson.Read<FootprintCatalogData>(stream);
            ArgumentNullException.ThrowIfNull(data.Footprints);
            if (data.SchemaVersion != 1 || data.Units != "mm" || data.Footprints.Count == 0)
                throw new ArgumentException("Неверная библиотека корпусов.");
            foreach (var footprint in data.Footprints)
            {
                ArgumentNullException.ThrowIfNull(footprint);
                ArgumentNullException.ThrowIfNull(footprint.Contacts);
                ArgumentNullException.ThrowIfNull(footprint.AllowedRotations);
                if (string.IsNullOrWhiteSpace(footprint.Id) || string.IsNullOrWhiteSpace(footprint.ComponentDefinitionId)
                    || footprint.Renderer is not ("arduino-uno" or "axial-resistor" or "led" or "button" or "placeholder"
                        or "arduino-nano" or "esp32-devkit" or "lcd1602" or "lcd1602-i2c" or "oled-ssd1306" or "seven-segment"
                        or "buzzer" or "servo" or "relay" or "capacitor" or "inductor" or "potentiometer" or "npn" or "diode"
                        or "dht11" or "dht22" or "hc-sr04" or "pir" or "photoresistor" or "thermistor" or "rgb-led" or "switch" or "battery" or SplitterGeometry.ComponentId)
                    || footprint.Mounting is not ("free" or "breadboard") || !double.IsFinite(footprint.Body.Width)
                    || !double.IsFinite(footprint.Body.Height) || footprint.Body.Width <= 0 || footprint.Body.Height <= 0
                    || footprint.AllowedRotations.Count == 0 || footprint.AllowedRotations.Any(angle => angle is not (0 or 90 or 180 or 270))
                    || footprint.Contacts.Any(contact => contact is null)
                    || !double.IsFinite(footprint.Body.X) || !double.IsFinite(footprint.Body.Y)
                    || footprint.Contacts.Select(contact => contact.Id).Distinct().Count() != footprint.Contacts.Count)
                    throw new ArgumentException("Неверное описание корпуса.");
                foreach (var contact in footprint.Contacts)
                    if (contact is null || string.IsNullOrWhiteSpace(contact.Id) || string.IsNullOrWhiteSpace(contact.ElectricalPinId)
                        || !double.IsFinite(contact.X) || !double.IsFinite(contact.Y)) throw new ArgumentException("Неверный контакт.");
                if (footprint.Renderer == SplitterGeometry.ComponentId &&
                    (footprint.Contacts.Count != SplitterGeometry.MaximumOutputs + 1 ||
                     footprint.Contacts.Select(c => c.Id).Where((id, index) => id != (index == 0 ? "IN" : $"OUT{index}")).Any() ||
                     footprint.Contacts.Select(c => c.ElectricalPinId).Distinct().Count() != 1))
                    throw new ArgumentException("Неверные гнёзда разветвителя.");
                if (footprint.Parts is { } parts && parts.Any(pair => string.IsNullOrWhiteSpace(pair.Key)
                    || !double.IsFinite(pair.Value.X) || !double.IsFinite(pair.Value.Y)
                    || !double.IsFinite(pair.Value.Width) || !double.IsFinite(pair.Value.Height)
                    || pair.Value.Width <= 0 || pair.Value.Height <= 0 || !footprint.Body.Contains(pair.Value)))
                    throw new ArgumentException("Неверные размеры частей корпуса.");
                if (footprint.RotaryControl is { } rotary && (!double.IsFinite(rotary.X) || !double.IsFinite(rotary.Y)
                    || !double.IsFinite(rotary.Radius) || rotary.Radius <= 0
                    || !footprint.Body.Contains(new CircuitEditor.Core.Layout.LayoutRect(rotary.X - rotary.Radius,
                        rotary.Y - rotary.Radius, rotary.Radius * 2, rotary.Radius * 2))))
                    throw new ArgumentException("Неверная геометрия ручки.");
                string[] required = footprint.Renderer switch
                {
                    "capacitor" => ["ceramic", "electrolytic"], "servo" => ["case", "left-ear", "right-ear", "shaft", "connector"],
                    "hc-sr04" => ["left-eye", "right-eye"], "pir" => ["lens"], "battery" => ["cell"], "switch" => ["slot"],
                    "rgb-led" => ["lens", "r-knee", "common-knee", "g-knee", "b-knee"], _ => []
                };
                if (required.Any(key => footprint.Parts?.ContainsKey(key) != true)
                    || footprint.Renderer == "potentiometer" && footprint.RotaryControl is null)
                    throw new ArgumentException("Отсутствует геометрия частей корпуса.");
                if (footprint.MinSpan.HasValue != footprint.MaxSpan.HasValue
                    || footprint.IsFlexible && (footprint.MinSpan < 1 || footprint.MaxSpan > 100 || footprint.MaxSpan < footprint.MinSpan
                        || footprint.DefaultSpan < footprint.MinSpan || footprint.DefaultSpan > footprint.MaxSpan || footprint.Contacts.Count != 2
                        || footprint.Contacts[0].X != 0 || footprint.Contacts[0].Y != 0 || footprint.Contacts[1].Y != 0))
                    throw new ArgumentException("Неверный диапазон шага ножек.");
            }
            if (data.Footprints.Select(item => item.Id).Distinct().Count() != data.Footprints.Count
                || data.Footprints.Select(item => item.ComponentDefinitionId).Distinct().Count() != data.Footprints.Count)
                throw new ArgumentException("Повторяющиеся корпуса.");
            return new(Array.AsReadOnly(data.Footprints.ToArray()), null);
        }
        catch (Exception error) when (EmbeddedJson.IsDataError(error)) { return new(null, "Корпуса: " + error.Message); }
    }
}
