namespace CircuitEditor.Core.Breadboards;

/// <summary>Развёрнутая из JSON топология одной модели breadboard.</summary>
public sealed record BreadboardDefinition(string Id, double Width, double Height, double HolePitch,
    IReadOnlyList<BreadboardHole> Holes, IReadOnlyList<ContactGroup> Groups)
{
    /// <summary>Проверяет внутреннее соединение двух существующих отверстий.</summary>
    public bool AreConnected(string first, string second)
    {
        var left = Holes.FirstOrDefault(hole => hole.Id == first);
        var right = Holes.FirstOrDefault(hole => hole.Id == second);
        return left is not null && right is not null && left.GroupId == right.GroupId;
    }
}
