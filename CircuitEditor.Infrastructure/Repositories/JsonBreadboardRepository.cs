using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Infrastructure.Data;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonBreadboardRepository : IBreadboardRepository
{
    public RepositoryResult<BreadboardDefinition> Load()
    {
        try { using var stream = EmbeddedJson.Open("breadboard.json"); return Read(stream); }
        catch (Exception error) when (EmbeddedJson.IsDataError(error)) { return new(null, error.Message); }
    }

    public static RepositoryResult<BreadboardDefinition> Read(Stream stream)
    {
        try
        {
            var data = EmbeddedJson.Read<BreadboardData>(stream);
            if (data.SchemaVersion != 1 || data.Units != "mm" || string.IsNullOrWhiteSpace(data.Id)
                || data.Body.Width <= 0 || data.Body.Height <= 0)
                throw new ArgumentException("Неверная версия или размеры breadboard.");
            ArgumentNullException.ThrowIfNull(data.TerminalBlocks); ArgumentNullException.ThrowIfNull(data.Rails);
            if (data.TerminalBlocks.Count == 0) throw new ArgumentException("Нет контактных блоков.");
            var holes = new List<BreadboardHole>(); var groups = new List<ContactGroup>();
            var pitches = new List<double>();
            foreach (var block in data.TerminalBlocks)
            {
                ArgumentNullException.ThrowIfNull(block);
                ArgumentNullException.ThrowIfNull(block.Columns);
                ValidateRows(block.Rows); pitches.Add(block.Rows.Pitch);
                if (block.Columns.Count == 0 || block.Connectivity != "perRow" || string.IsNullOrWhiteSpace(block.Id))
                    throw new ArgumentException("Неверный контактный блок.");
                for (var index = 0; index < block.Rows.Count; index++)
                {
                    var row = block.Rows.First + index;
                    var group = $"{block.Id}/row/{row:D2}";
                    var members = new List<string>();
                    foreach (var column in block.Columns)
                    {
                        if (column is null || string.IsNullOrWhiteSpace(column.Id)) throw new ArgumentException("Нет столбца.");
                        var id = $"{block.Id}/{column.Id}/{row:D2}";
                        members.Add(id);
                        holes.Add(new(id, new LayoutPoint(column.X, block.Rows.StartY + index * block.Rows.Pitch), group));
                    }
                    groups.Add(new(group, members.AsReadOnly()));
                }
            }
            foreach (var rail in data.Rails)
            {
                ArgumentNullException.ThrowIfNull(rail); ArgumentNullException.ThrowIfNull(rail.Segments);
                if (rail.Segments.Any(segment => segment is null) || string.IsNullOrWhiteSpace(rail.Id))
                    throw new ArgumentException("Неверная шина питания.");
                ValidateRows(rail.Holes); pitches.Add(rail.Holes.Pitch);
                for (var index = 0; index < rail.Holes.Count; index++)
                {
                    var row = rail.Holes.First + index;
                    var segments = rail.Segments.Where(segment => segment is not null && row >= segment.First && row <= segment.Last).ToArray();
                    if (segments.Length != 1) throw new ArgumentException("Отверстие шины должно принадлежать одному сегменту.");
                    var group = $"{rail.Id}/{segments[0].Id}";
                    holes.Add(new($"{rail.Id}/{row:D2}", new LayoutPoint(rail.X, rail.Holes.StartY + index * rail.Holes.Pitch), group));
                }
                foreach (var segment in rail.Segments)
                {
                    var group = $"{rail.Id}/{segment.Id}";
                    var members = holes.Where(hole => hole.GroupId == group).Select(hole => hole.Id).ToArray();
                    if (members.Length == 0) throw new ArgumentException("Пустой сегмент шины.");
                    groups.Add(new(group, members));
                }
            }
            if (pitches.Distinct().Count() != 1 || holes.Select(hole => hole.Id).Distinct().Count() != holes.Count
                || groups.Select(group => group.Id).Distinct().Count() != groups.Count
                || holes.Select(hole => hole.Position).Distinct().Count() != holes.Count
                || holes.Any(hole => !double.IsFinite(hole.Position.X) || !double.IsFinite(hole.Position.Y)
                    || hole.Position.X < 0 || hole.Position.Y < 0 || hole.Position.X > data.Body.Width || hole.Position.Y > data.Body.Height))
                throw new ArgumentException("Повторяющиеся отверстия/группы или неверные координаты.");
            return new(new BreadboardDefinition(data.Id, data.Body.Width, data.Body.Height, pitches[0],
                holes.AsReadOnly(), groups.AsReadOnly()), null);
        }
        catch (Exception error) when (EmbeddedJson.IsDataError(error)) { return new(null, "Топология breadboard: " + error.Message); }
    }
    private static void ValidateRows(RowData rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count is < 1 or > 200 || rows.First < 0 || !double.IsFinite(rows.Pitch) || rows.Pitch <= 0
            || !double.IsFinite(rows.StartY)) throw new ArgumentException("Неверные ряды отверстий.");
    }
}
