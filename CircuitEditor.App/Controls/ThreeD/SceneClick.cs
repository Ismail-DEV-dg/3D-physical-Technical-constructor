namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneClick
{
    private const double Distance = 4;
    private (double X, double Y)? start;
    private bool moved;

    public void Begin(double x, double y) { start = (x, y); moved = false; }

    public void Move(double x, double y)
    {
        if (start is { } p)
            moved |= (p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y) >= Distance * Distance;
    }

    public bool Release(double x, double y)
    {
        Move(x, y);
        var result = start is not null && !moved;
        Cancel();
        return result;
    }

    public void Cancel() => start = null;
}
