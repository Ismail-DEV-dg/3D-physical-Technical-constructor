namespace CircuitEditor.Core.Scene;

/// <summary>Высота корпуса, зазор над опорой и материал, дополняющие двумерный footprint.</summary>
public sealed record SceneProfile(SceneShape Shape, double Height, double Clearance, string Material,
    double DetailHeight = 1);
