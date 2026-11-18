namespace CircuitEditor.Core.Breadboards;

/// <summary>Набор внутренне соединённых отверстий; проводов симуляции здесь нет.</summary>
public sealed record ContactGroup(string Id, IReadOnlyList<string> HoleIds);
