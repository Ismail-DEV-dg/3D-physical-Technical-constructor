using CircuitEditor.Core.Resistance;

namespace CircuitEditor.Core.Physical;

/// <summary>Точная четырёх- или пятиполосная маркировка процедурного резистора.</summary>
public static class ResistorMarking
{
    private static readonly string[] Digits = ["#171717", "#704020", "#D32F2F", "#F27822", "#F7D12E",
        "#238B45", "#2466D6", "#843DB0", "#7D8790", "#F2F2EC"];
    private const string Gold = "#C9A438";
    private const string Silver = "#B8BDC7";
    /// <summary>Возвращает полосы без округления; для трёх значащих цифр использует пять полос.</summary>
    public static IReadOnlyList<string> GetBands(string value, double tolerancePercent = 5, int? bandCount = null)
    {
        if (!ResistanceFormatter.TryParse(value, out var number)) return [];
        foreach (var significantDigits in new[] { 2, 3 })
        {
            if(bandCount is not null && significantDigits!=bandCount-2)continue;
            var exponent = (int)Math.Floor(Math.Log10((double)number)) - significantDigits + 1;
            if (exponent is < -2 or > 9) continue;
            var scale = (decimal)Math.Pow(10, exponent);
            var digits = number / scale;
            if (digits != decimal.Truncate(digits)) continue;
            var code = (int)digits;
            var bands = new List<string>();
            if (significantDigits == 3) { bands.Add(Digits[code / 100]); code %= 100; }
            bands.Add(Digits[code / 10]); bands.Add(Digits[code % 10]);
            bands.Add(exponent == -1 ? Gold : exponent == -2 ? Silver : Digits[exponent]);
            bands.Add(Tolerances.FirstOrDefault(p=>p.Value==tolerancePercent).Key ?? Gold);
            return bands;
        }
        return [];
    }
    private static readonly IReadOnlyDictionary<string,double> Tolerances=new Dictionary<string,double>
    { [Gold]=5,[Silver]=10,[Digits[1]]=1,[Digits[2]]=2,[Digits[5]]=.5,[Digits[6]]=.25,[Digits[7]]=.1,[Digits[8]]=.05 };
    /// <summary>Читает четыре или пять цветовых полос; список цветов совпадает с рисовальщиком.</summary>
    public static Electronics.CalculationResult<Electronics.BandResistance> Decode(IReadOnlyList<string> bands)
    {
        if(bands.Count is not (4 or 5)||!Tolerances.TryGetValue(bands[^1],out var tolerance))return new(null,"Нужны 4/5 полос и корректный допуск.");
        var code=0;
        foreach(var band in bands.Take(bands.Count-2)){var digit=Array.IndexOf(Digits,band);if(digit<0)return new(null,"Неизвестный цвет значащей цифры.");code=code*10+digit;}
        var multiplier=bands[^2]==Gold ? -1 : bands[^2]==Silver ? -2 : Array.IndexOf(Digits,bands[^2]);
        if(multiplier<0 && bands[^2]!=Gold && bands[^2]!=Silver)return new(null,"Неизвестный множитель.");
        return new(new(code*(decimal)Math.Pow(10,multiplier),tolerance),null);
    }
    /// <summary>Цвета для выбора полос.</summary>
    public static IReadOnlyList<string> Colors=>Digits.Concat(new[]{Gold,Silver}).ToArray();
}
