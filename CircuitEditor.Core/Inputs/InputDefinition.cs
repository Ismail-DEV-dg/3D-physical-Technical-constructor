using System.Globalization;

namespace CircuitEditor.Core.Inputs;

/// <summary>Диапазон, единица и допустимые варианты входного условия, без зависимости от UI.</summary>
public sealed record InputDefinition(ComponentInput Input, string Label, double Minimum, double Maximum,
    double Step, double Default, string Unit, IReadOnlyList<double>? Choices = null, IReadOnlyList<string>? ChoiceLabels = null)
{
    /// <summary>Проверяет границы, конечность и принадлежность дискретному списку.</summary>
    public bool IsValid(double value) => double.IsFinite(value) && value >= Minimum && value <= Maximum
        && (Choices is null || Choices.Contains(value));
    /// <summary>Форматирует значение или название варианта.</summary>
    public string Format(double value)
    {
        if (Choices is not null && ChoiceLabels is not null)
            for (var index = 0; index < Choices.Count; index++)
                if (Choices[index] == value) return ChoiceLabels[index];
        return value.ToString("0.##", CultureInfo.InvariantCulture) + (Unit.Length == 0 ? "" : " " + Unit);
    }
    /// <summary>Подпись допустимого диапазона.</summary>
    public string RangeText => $"{Format(Minimum)} … {Format(Maximum)}";
}
