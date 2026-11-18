using System.Collections.ObjectModel;
using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Electronics;
/// <summary>Параметры только применимой модели и будущие роли выводов экземпляра.</summary>
public sealed class ComponentElectricalState
{
    /// <summary>Создаёт независимые сериализуемые параметры, без библиотечных значений по умолчанию.</summary>
    public ComponentElectricalState(IReadOnlyDictionary<string,double>? parameters=null,IReadOnlyDictionary<string,PinRole>? pinRoles=null)
    {
        Parameters=new ReadOnlyDictionary<string,double>(parameters?.ToDictionary()??[]);
        PinRoles=new ReadOnlyDictionary<string,PinRole>(pinRoles?.ToDictionary()??[]);
    }
    /// <summary>Явно заданные значения; отсутствие Vf означает профиль цвета из JSON.</summary>
    public IReadOnlyDictionary<string,double> Parameters {get;}
    /// <summary>Роли на этапы 6–7. Отсутствующая роль означает Unknown.</summary>
    public IReadOnlyDictionary<string,PinRole> PinRoles {get;}
    /// <summary>Проверяет применимость и положительные конечные значения.</summary>
    public bool IsValid(ComponentDefinition definition)
    {
        var keys=definition.Id switch {"led"=>new[]{"Vf","DynamicResistance","MaximumCurrent"},"rgb-led"=>["R.Vf","G.Vf","B.Vf","DynamicResistance","MaximumCurrent"],"thermistor"=>["R25","Beta"],"photoresistor"=>["LightResistance","DarkResistance"],_=>Array.Empty<string>()};
        return Parameters.All(p=>keys.Contains(p.Key)&&double.IsFinite(p.Value)&&p.Value>0)
            && PinRoles.All(p=>Enum.IsDefined(p.Value)&&definition.Pins.Any(pin=>pin.Id==p.Key));
    }
    /// <summary>Разрешает неизвестную роль без угадывания направления.</summary>
    public PinRole Role(string pin)=>PinRoles.GetValueOrDefault(pin,PinRole.Unknown);
}
