using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Chips;

/// <summary>Описание цифрового поведения.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ChipTruthTable), "truthTable")]
[JsonDerivedType(typeof(ChipGraph), "graph")]
public abstract record ChipBehaviorDefinition;
