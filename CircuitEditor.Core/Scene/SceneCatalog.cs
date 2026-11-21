namespace CircuitEditor.Core.Scene;

/// <summary>Каталог профилей, индексируемый по ключу рисовальщика footprint.</summary>
public sealed record SceneCatalog(IReadOnlyDictionary<string, SceneProfile> Profiles,
    SceneProfile Breadboard, SceneProfile Fallback, SceneWireProfile Wire);
