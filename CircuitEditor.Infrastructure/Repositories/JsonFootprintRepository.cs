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
                        or "arduino-nano" or "esp32-devkit" or "lcd1602" or "lcd1602-i2c" or "oled-ssd1306" or "seven-segment")
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
