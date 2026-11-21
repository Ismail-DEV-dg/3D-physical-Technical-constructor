using System.Globalization;
using CircuitEditor.Core.Code;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchSymbols
{
    private readonly List<Dictionary<string, (string Source, int? Slot)>> scopes = [new(StringComparer.Ordinal)];
    private readonly List<long> initialValues = [];
    private readonly HashSet<int> constantUses = [];
    private readonly HashSet<int> writes = [];

    public IReadOnlyList<long> InitialValues => initialValues.ToArray();

    public void Enter() => scopes.Add(new(StringComparer.Ordinal));
    public void Leave() => scopes.RemoveAt(scopes.Count - 1);

    public void Declare(CppToken name, CppToken value, bool mutable)
    {
        if (!char.IsAsciiLetterOrDigit(value.Text.FirstOrDefault()) && !value.Text.StartsWith('_'))
            throw new SketchSyntaxException(value, "Значение константы должно быть числом или именем.");
        var slot = mutable && Scalar(Resolve(value, constantUse: true)) is long initial ? Allocate(initial) : (int?)null;
        if (!scopes[^1].TryAdd(name.Text, (value.Text, slot)))
            throw new SketchSyntaxException(name, $"Имя «{name.Text}» уже объявлено в этом блоке.");
    }

    public int? Slot(CppToken token) => Find(token.Text)?.Slot;

    public int WritableSlot(CppToken token)
    {
        var slot = Slot(token);
        if (slot is null || constantUses.Contains(slot.Value))
            throw new SketchSyntaxException(token, $"«{token.Text}» должно быть изменяемой числовой или логической переменной.");
        writes.Add(slot.Value);
        return slot.Value;
    }

    public string Resolve(CppToken token, bool constantUse)
    {
        var value = token.Text;
        var remaining = scopes.Sum(scope => scope.Count);
        while (Find(value) is { } symbol)
        {
            if (remaining-- == 0) throw new SketchSyntaxException(token, $"Циклическая ссылка «{token.Text}».");
            if (constantUse && symbol.Slot is int slot)
            {
                if (writes.Contains(slot)) throw new SketchSyntaxException(token, $"Для «{token.Text}» требуется постоянное значение.");
                constantUses.Add(slot);
            }
            value = symbol.Source;
        }
        return value;
    }

    public static long? Scalar(string value)
    {
        if (value is "LOW" or "false") return 0;
        if (value is "HIGH" or "true") return 1;
        var digits = value.TrimEnd('u', 'U', 'l', 'L');
        var hex = digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (hex) digits = digits[2..];
        return long.TryParse(digits, hex ? NumberStyles.HexNumber : NumberStyles.None, CultureInfo.InvariantCulture, out var result)
            ? result : null;
    }

    private (string Source, int? Slot)? Find(string name)
    {
        for (var i = scopes.Count - 1; i >= 0; i--)
            if (scopes[i].TryGetValue(name, out var value)) return value;
        return null;
    }

    private int Allocate(long value)
    {
        initialValues.Add(value);
        return initialValues.Count - 1;
    }
}
