using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Validation;
/// <summary>Обязательное питание модулей; питающие выходы Arduino/ESP32 исключены данными.</summary>
public sealed class DisconnectedSupplyRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c)
    {
        foreach(var component in c.Project.Circuit.Components.Where(x=>c.Catalog.Find(x.Definition.Id)?.RequiresPower==true))
            foreach(var pin in component.Definition.Pins.Where(p=>p.IsExposed && p.Type is PinType.Power or PinType.Ground))
                if(!c.Nets.Connected(component.Id,pin.Id))
                    yield return ErcContext.Issue("DISCONNECTED_SUPPLY",$"{component.Reference}: не подключён вывод питания/земли {pin.Name}.",[new(component.Id,pin.Id)],
                        [c.Nets.Find(component.Id,pin.Id)!.Id],ErcSeverity.Warning);
    }
}
