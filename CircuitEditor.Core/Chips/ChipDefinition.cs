using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Chips;

/// <summary>Версионируемое определение цифровой микросхемы с физическим корпусом DIP.</summary>
public sealed record ChipDefinition(Guid Id, int DefinitionVersion, string Name, string Description,
    string Package, string Family, IReadOnlyList<ChipPin> Pins, ChipBehaviorDefinition Behavior,
    long PropagationDelayUs = 0)
{
    /// <summary>Уникальный ключ версии в электрической модели.</summary>
    [JsonIgnore]
    public string ComponentId => FormattableString.Invariant($"chip:{Id:D}:{DefinitionVersion}");
}
