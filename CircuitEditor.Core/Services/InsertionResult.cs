using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Core.Services;

/// <summary>Атомарный результат проверки вставки без частичных привязок.</summary>
public sealed record InsertionResult(bool Success, string? Error, ComponentPlacement? Placement,
    IReadOnlyList<ContactBinding> Bindings);
