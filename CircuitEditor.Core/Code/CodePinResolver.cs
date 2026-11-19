using System.Globalization;
namespace CircuitEditor.Core.Code;
/// <summary>Нормализует числа, A/D/GPIO-имена и простые цепочки констант.</summary>
public static class CodePinResolver
{
    /// <summary>Возвращает физический пин таблицы; не вычисляет выражения.</summary>
    public static BoardPinDefinition? Resolve(string value, CodeBoardProfile profile, IReadOnlyDictionary<string,string> aliases)
    {
        var seen=new HashSet<string>();
        while(aliases.TryGetValue(value,out var next)){if(!seen.Add(value))return null;value=next;}
        var named=profile.Pins.FirstOrDefault(p=>p.Id==value);if(named is not null)return named;
        if(value=="LED_BUILTIN")return profile.Pins.FirstOrDefault(p=>p.Number==profile.BuiltinLedNumber);
        var digits=value.TrimEnd('u','U','l','L');
        var hex=digits.StartsWith("0x",StringComparison.OrdinalIgnoreCase);
        if(hex)digits=digits[2..];
        return int.TryParse(digits,hex?NumberStyles.HexNumber:NumberStyles.Integer,CultureInfo.InvariantCulture,out var number)
            ? profile.Pins.FirstOrDefault(p=>p.Number==number) : null;
    }
}
