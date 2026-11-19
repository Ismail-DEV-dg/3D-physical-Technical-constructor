using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Simulation;

/// <summary>Настройки .circ v3. Снимки, время, состояния GPIO, зонды и история приборов принадлежат сеансу.</summary>
public sealed record SimulationSetup
{
    /// <summary>Выбранный сценарий или ручное управление при null.</summary>
    public ScenarioConfiguration? Scenario { get; init; }

    /// <summary>Последние позиции панелей по типу прибора.</summary>
    public ImmutableDictionary<InstrumentKind, InstrumentLayout> Instruments { get; init; } =
        ImmutableDictionary<InstrumentKind, InstrumentLayout>.Empty;

    /// <summary>Проверяет форму данных. Существование привязок проверяется при запуске сценария.</summary>
    [JsonIgnore]
    public bool IsValid => (Scenario is null || Scenario.IsValid) &&
        Instruments is not null && Instruments.All(pair => Enum.IsDefined(pair.Key) && pair.Value?.IsValid == true);

    /// <summary>Сравнивает содержимое для признака несохранённых изменений.</summary>
    public bool Matches(SimulationSetup other)
    {
        if (!Same(Instruments, other.Instruments)) return false;
        if (Scenario is null || other.Scenario is null) return Scenario is null && other.Scenario is null;
        return Scenario.TemplateId == other.Scenario.TemplateId && Scenario.TargetId == other.Scenario.TargetId &&
            Scenario.Text == other.Scenario.Text && Same(Scenario.Pins, other.Scenario.Pins) &&
            Same(Scenario.Numbers, other.Scenario.Numbers);
    }

    private static bool Same<TKey, TValue>(ImmutableDictionary<TKey, TValue> first,
        ImmutableDictionary<TKey, TValue> second) where TKey : notnull =>
        first.Count == second.Count && first.All(pair => second.TryGetValue(pair.Key, out var value) &&
            EqualityComparer<TValue>.Default.Equals(pair.Value, value));
}
