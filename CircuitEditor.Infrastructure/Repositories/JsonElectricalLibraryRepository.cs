using System.Text.Json;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class JsonElectricalLibraryRepository : IElectricalLibraryRepository
{
    public RepositoryResult<ElectricalCatalog> Load()
    {
        try
        {
            using var stream = typeof(JsonElectricalLibraryRepository).Assembly.GetManifestResourceStream("CircuitEditor.Infrastructure.Data.electrical.json")!;
            var data=JsonSerializer.Deserialize<ElectricalCatalog>(stream,new JsonSerializerOptions {PropertyNameCaseInsensitive=true})!;
            if(data is null || data.Components is null || data.LedProfiles is null || data.SchemaVersion!=1 || data.Components.Count==0 || data.Components.Select(c=>c.ComponentId).Distinct().Count()!=data.Components.Count
                || data.Components.Any(c=>string.IsNullOrWhiteSpace(c.ComponentId)||c.Links is null||c.Sources is null
                    || c.Model is not ("resistor" or "source" or "led" or "rgb-led" or "switch" or "potentiometer" or "ldr" or "ntc" or "unsupported")
                    || c.Links.Any(l=>l.Pins is null||l.Pins.Count<2||l.Input is not null&&!Enum.TryParse<CircuitEditor.Core.Inputs.ComponentInput>(l.Input,out _))
                    || c.Sources.Any(s=>!double.IsFinite(s.Voltage)||s.Voltage<=0))
                || data.LedProfiles.Count==0 || data.LedProfiles.Select(p=>p.Id).Distinct().Count()!=data.LedProfiles.Count
                || data.LedProfiles.Any(p=>!CircuitEditor.Core.Models.ComponentProperties.IsColor(p.Color)
                    || new[]{p.Vf,p.DynamicResistance,p.MaximumCurrent}.Any(v=>!double.IsFinite(v)||v<=0)))
                throw new ArgumentException("Некорректная электрическая библиотека.");
            return new(data,null);
        }
        catch(Exception e) when(e is JsonException or ArgumentException or InvalidOperationException or IOException or NullReferenceException or NotSupportedException)
        { return new(null,"Не удалось загрузить электрические модели: "+e.Message); }
    }
}
