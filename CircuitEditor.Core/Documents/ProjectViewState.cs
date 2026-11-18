using CircuitEditor.Core.Placement;
namespace CircuitEditor.Core.Documents;
/// <summary>Сохраняемые камеры обоих представлений.</summary>
public sealed record ProjectViewState(RepresentationKind Active = RepresentationKind.Mockup, double MockupZoom = 1,
    double MockupX = 16, double MockupY = 12, double SchematicZoom = 1, double SchematicX = 40, double SchematicY = 40)
{
    /// <summary>Минимальный масштаб сохраняемого представления.</summary>
    public const double MinimumZoom = .25;
    /// <summary>Максимальный масштаб сохраняемого представления.</summary>
    public const double MaximumZoom = 3;
}
