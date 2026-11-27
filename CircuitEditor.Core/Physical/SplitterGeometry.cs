using System.Runtime.CompilerServices;

namespace CircuitEditor.Core.Physical;

/// <summary>Варианты корпуса пассивного разветвителя с общей шиной.</summary>
public static class SplitterGeometry
{
    /// <summary>Идентификатор разветвителя в библиотеке.</summary>
    public const string ComponentId = "splitter";
    /// <summary>Минимальное число ответвлений.</summary>
    public const int MinimumOutputs = 2;
    /// <summary>Максимальное число ответвлений.</summary>
    public const int MaximumOutputs = 8;
    /// <summary>Число ответвлений новой детали.</summary>
    public const int DefaultOutputs = 3;
    /// <summary>Глубина гнезда трёхмерной модели в миллиметрах.</summary>
    public const double SocketDepth = 1.8;

    private static readonly ConditionalWeakTable<FootprintDefinition, FootprintDefinition[]> Variants = new();

    /// <summary>Возвращает корпус с одним входным и заданным числом выходных гнёзд.</summary>
    public static FootprintDefinition? Apply(FootprintDefinition? footprint, int? outputs)
    {
        if (footprint is null || outputs is not int count) return footprint;
        return Variants.GetValue(footprint, template => Enumerable.Range(MinimumOutputs, MaximumOutputs - MinimumOutputs + 1)
            .Select(outputs => Build(template, outputs)).ToArray())[count - MinimumOutputs];
    }

    private static FootprintDefinition Build(FootprintDefinition footprint, int outputs)
    {
        var contacts = Array.AsReadOnly(footprint.Contacts.Take(outputs + 1).ToArray());
        var endMargin = footprint.Body.Right - footprint.Contacts[^1].X;
        return footprint with
        {
            Contacts = contacts,
            Body = footprint.Body with { Width = contacts[^1].X - footprint.Body.X + endMargin }
        };
    }
}
