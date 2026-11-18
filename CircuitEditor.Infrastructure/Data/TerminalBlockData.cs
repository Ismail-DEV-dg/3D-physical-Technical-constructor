namespace CircuitEditor.Infrastructure.Data;

internal sealed record TerminalBlockData(string Id, IReadOnlyList<ColumnData> Columns, RowData Rows, string Connectivity);
