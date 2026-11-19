using System.Collections.Immutable;
using System.Text.Json.Serialization;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Simulation;

/// <summary>Выбранный сценарий, стабильные привязки и пользовательские параметры.</summary>
public sealed record ScenarioConfiguration(string TemplateId, ImmutableDictionary<string, PinReference> Pins,
    ImmutableDictionary<string, double> Numbers, Guid? TargetId = null, string? Text = null)
{
    /// <summary>Максимальная длина строки пользовательского сценария.</summary>
    public const int MaximumTextLength = 512;

    /// <summary>Проверяет форму файла, сохраняя ссылки на удалённые сущности.</summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(TemplateId) && TemplateId.Length <= 80 &&
        TargetId != Guid.Empty && (Text is null || Text.Length <= MaximumTextLength) &&
        Pins is not null && Pins.All(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null &&
            pair.Value.ComponentId != Guid.Empty && !string.IsNullOrWhiteSpace(pair.Value.PinId)) &&
        Numbers is not null && Numbers.All(pair => !string.IsNullOrWhiteSpace(pair.Key) && double.IsFinite(pair.Value));
}
