using System.Globalization;
using System.Text.RegularExpressions;
using CircuitEditor.Core.Resistance;
namespace CircuitEditor.Core.Electronics;
/// <summary>Ввод и вывод величин с единицами СИ и принятыми приставками.</summary>
public static class QuantityFormatter
{
    /// <summary>Читает число с единицей U/I/R/P; единица обязательна.</summary>
    public static bool TryParse(string? text,string quantity,out double value)
    {
        value=0;if(quantity=="R"){var ok=ResistanceFormatter.TryParse(text,out var r);value=(double)r;return ok;}
        var match=Regex.Match(text??"",@"^\s*([+-]?\d+(?:[.,]\d+)?)\s*(мк|µ|u|м|m|к|k)?(В|V|А|A|Вт|W)\s*$");
        if(!match.Success || !double.TryParse(match.Groups[1].Value.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value))return false;
        var unit=match.Groups[3].Value;if(quantity=="U" && unit is not ("В" or "V") || quantity=="I" && unit is not ("А" or "A") || quantity=="P" && unit is not ("Вт" or "W"))return false;
        value*=match.Groups[2].Value switch {"мк" or "µ" or "u"=>1e-6,"м" or "m"=>1e-3,"к" or "k"=>1e3,_=>1};return double.IsFinite(value);
    }
    /// <summary>Форматирует также сопротивления вне decimal без переполнения в калькуляторе.</summary>
    public static string Resistance(double value)=>double.IsFinite(value)&&Math.Abs(value)<=(double)decimal.MaxValue
        ? ResistanceFormatter.Format((decimal)value) : value.ToString("G6",CultureInfo.InvariantCulture)+" Ω";
    /// <summary>Форматирует значение без фиктивного нуля для неизвестного узла.</summary>
    public static string Format(double? value,string unit)
    {
        if(value is null)return "не определено";
        var v=value.Value;var small=Math.Abs(v)>0&&Math.Abs(v)<1;
        return (small?v*1000:v).ToString("0.###",CultureInfo.InvariantCulture)+" "+(small?"м":"")+unit;
    }
}
