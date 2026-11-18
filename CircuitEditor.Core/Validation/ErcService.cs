using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Core.Validation;
/// <summary>Агрегирует независимые правила и ошибки исходных данных.</summary>
public sealed class ErcService(IElectricalLibraryRepository repository,IEnumerable<IErcRule> rules) : IErcService
{
    /// <inheritdoc/>
    public IReadOnlyList<ErcIssue> Check(CircuitProject project, Netlist.Netlist nets)
    {
        var data=repository.Load();
        if(data.Value is null) return [new(ErcSeverity.Error,"LIBRARY",data.Error ?? "Нет электрической библиотеки.",[],[],[])];
        var context=new ErcContext(project,nets,data.Value);
        return nets.Diagnostics.Select(d=>new ErcIssue(ErcSeverity.Error,"NETLIST",d,[],[],[]))
            .Concat(rules.SelectMany(r=>r.Check(context))).ToArray();
    }
}
