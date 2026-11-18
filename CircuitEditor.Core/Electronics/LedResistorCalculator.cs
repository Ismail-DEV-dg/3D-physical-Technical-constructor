using CircuitEditor.Core.Resistance;
namespace CircuitEditor.Core.Electronics;
/// <summary>Подбор стандартного номинала вверх с профилем LED из библиотеки.</summary>
public static class LedResistorCalculator
{
    /// <summary>Использует формулу (U−Vf)/I; динамическое сопротивление учитывается DC-решателем.</summary>
    public static CalculationResult<LedResistorResult> Calculate(double voltage,double current,LedProfile profile,ResistorSeriesKind series)
    {
        if(!double.IsFinite(voltage)||!double.IsFinite(current)||current<=0||voltage<=profile.Vf)return new(null,"Питание должно превышать Vf, ток — быть положительным.");
        var wanted=(voltage-profile.Vf)/current;
        if(!double.IsFinite(wanted)||wanted>(double)ResistorSeries.MaximumOhms)return new(null,"Номинал вне доступного ряда.");
        var resistance=ResistorSeries.GetValues(series).First(v=>(double)v>=wanted);
        var actual=(voltage-profile.Vf)/(double)resistance;
        return new(new((double)resistance,actual,actual*actual*(double)resistance,profile.Vf),null);
    }
}
