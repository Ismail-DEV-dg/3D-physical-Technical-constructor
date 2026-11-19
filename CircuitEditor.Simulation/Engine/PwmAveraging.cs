using CircuitEditor.Core.Electronics;

namespace CircuitEditor.Simulation.Engine;

internal static class PwmAveraging
{
    internal static DcResult Combine(IReadOnlyList<(double Start, DcResult Result)> phases)
    {
        if (phases.Count == 1) return phases[0].Result;
        double? Average(Func<DcResult, double?> get)
        {
            var sum = 0d;
            for (var i = 0; i < phases.Count; i++)
            {
                var end = i + 1 < phases.Count ? phases[i + 1].Start : 1;
                if (get(phases[i].Result) is not double value) return null;
                sum += value * (end - phases[i].Start);
            }
            return sum;
        }

        var voltages = phases[0].Result.Voltages.Keys.ToDictionary(id => id, id => Average(r => r.Voltages.GetValueOrDefault(id)));
        var branchMaps = phases.ToDictionary(p => p.Result, p => p.Result.Branches.ToDictionary(b => b.Name));
        var branches = phases.SelectMany(p => p.Result.Branches).DistinctBy(b => b.Name).Select(branch => branch with
        {
            Voltage = Average(r => branchMaps[r].GetValueOrDefault(branch.Name)?.Voltage) ?? branch.Voltage,
            Current = Average(r => branchMaps[r].GetValueOrDefault(branch.Name)?.Current),
            Power = Average(r => branchMaps[r].GetValueOrDefault(branch.Name)?.Power)
        }).ToArray();
        return new(phases.All(p => p.Result.Success), voltages, branches,
            phases.SelectMany(p => p.Result.Diagnostics).Distinct().ToArray(),
            phases.SelectMany(p => p.Result.UnsupportedComponents).Distinct().ToArray());
    }
}
