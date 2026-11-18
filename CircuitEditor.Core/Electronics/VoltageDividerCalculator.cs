namespace CircuitEditor.Core.Electronics;
/// <summary>Ненагруженный резистивный делитель.</summary>
public static class VoltageDividerCalculator
{
    /// <summary>Возвращает Uout между средней точкой и землёй.</summary>
    public static double? Calculate(double voltage,double upper,double lower)=>new[]{voltage,upper,lower}.All(double.IsFinite)
        && voltage>=0 && upper>0 && lower>0 ? voltage*(lower/Math.Max(upper,lower))/(upper/Math.Max(upper,lower)+lower/Math.Max(upper,lower)) : null;
}
