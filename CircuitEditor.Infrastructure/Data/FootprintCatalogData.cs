using CircuitEditor.Core.Physical;

namespace CircuitEditor.Infrastructure.Data;

internal sealed record FootprintCatalogData(int SchemaVersion, string Units, IReadOnlyList<FootprintDefinition> Footprints);
