using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Chips;

/// <summary>Описание цифрового поведения.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ChipTruthTable), "truthTable")]
public abstract record ChipBehaviorDefinition;
