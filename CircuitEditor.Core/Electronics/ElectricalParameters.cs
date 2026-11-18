using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Electronics;
/// <summary>Общие эффективные параметры для ERC, расчёта и инспектора.</summary>
public static class ElectricalParameters
{
    /// <summary>Цветовой профиль с явными переопределениями экземпляра.</summary>
    public static LedProfile Led(Component c,ElectricalCatalog catalog,string? channel=null)
    {
        var p=channel is null ? catalog.Led(c.Color) : catalog.LedProfiles.Single(p=>p.Id==catalog.Find(c.Definition.Id)!.ChannelProfiles![channel]);var v=c.Electrical.Parameters;
        return p with {Vf=v.GetValueOrDefault(channel is null ? "Vf" : channel+".Vf",p.Vf),DynamicResistance=v.GetValueOrDefault("DynamicResistance",p.DynamicResistance),MaximumCurrent=v.GetValueOrDefault("MaximumCurrent",p.MaximumCurrent)};
    }
}
