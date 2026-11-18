namespace CircuitEditor.Infrastructure.Data;

internal sealed record RailData(string Id, double X, RowData Holes, IReadOnlyList<RailSegmentData> Segments);
