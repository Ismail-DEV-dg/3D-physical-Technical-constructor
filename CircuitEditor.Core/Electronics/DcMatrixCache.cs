namespace CircuitEditor.Core.Electronics;

/// <summary>Ограниченный кэш коэффициентов внутри одного сеанса DC.</summary>
public sealed class DcMatrixCache
{
    private const int Capacity = 32;
    private readonly Dictionary<string, double[,]> matrices = new(StringComparer.Ordinal);
    /// <summary>Количество сборок коэффициентов, включая вытесненные записи.</summary>
    public int BuildCount { get; private set; }

    internal bool TryGet(string key, out double[,]? matrix) => matrices.TryGetValue(key, out matrix);

    internal void Add(string key, double[,] matrix)
    {
        if (matrices.Count >= Capacity) matrices.Clear();
        matrices.Add(key, matrix);
        BuildCount++;
    }

    /// <summary>Удаляет матрицы предыдущей топологии.</summary>
    public void Clear() => matrices.Clear();
}
