using System.Collections.Immutable;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Core.Scene;

/// <summary>Один объект макета с локальными деталями либо одна перемычка с трассой в координатах стола.</summary>
public sealed record SceneNode(Guid Id, SceneLayer Layer, string Family, SceneProfile Profile, ScenePose Pose,
    LayoutRect Body, SceneBounds Bounds, ImmutableArray<SceneContact> Contacts, ImmutableArray<ScenePoint> Path,
    ImmutableDictionary<string, LayoutRect> Parts, string Label = "", string Name = "", string Value = "",
    string? Color = null, string? Variant = null, double WireRadius = 0, double Tolerance = 0,
    LayoutPoint? Knob = null, double KnobRadius = 0, double Angle = 0, double SwitchPosition = 0,
    FootprintDefinition? Footprint = null, BreadboardDefinition? Breadboard = null);
