namespace CircuitEditor.Core.Electronics;
/// <summary>Типичные параметры LED; значения загружаются из JSON.</summary>
public sealed record LedProfile(string Id, string Name, string Color, double Vf, double DynamicResistance, double MaximumCurrent)
{
    /// <summary>Читаемое имя профиля в списках выбора.</summary>
    public override string ToString()=>Name;
}
