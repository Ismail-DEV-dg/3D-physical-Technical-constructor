using CircuitEditor.Core.Layout;

namespace CircuitEditor.Infrastructure.Data;

internal sealed record BreadboardData(int SchemaVersion, string Id, string Units, LayoutRect Body,
    IReadOnlyList<TerminalBlockData> TerminalBlocks, IReadOnlyList<RailData> Rails);
