namespace CircuitEditor.Core.Scene;

/// <summary>Точка в миллиметрах. X и Y совпадают с «Макетом», Z растёт от стола вверх.</summary>
public readonly record struct ScenePoint(double X, double Y, double Z);
