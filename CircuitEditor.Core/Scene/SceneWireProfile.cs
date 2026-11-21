namespace CircuitEditor.Core.Scene;

/// <summary>Общий радиус изоляции и параметры подъёма перемычек в трёхмерном виде.</summary>
public sealed record SceneWireProfile(double Radius, double MinimumRise, double MaximumRise, double RisePerMillimeter);
