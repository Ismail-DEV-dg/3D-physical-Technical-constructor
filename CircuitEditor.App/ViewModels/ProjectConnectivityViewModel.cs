using CircuitEditor.Core.Netlist;
namespace CircuitEditor.App.ViewModels;
public sealed class ProjectConnectivityViewModel(ProjectViewModel project, INetlistBuilder? builder)
{
    public Netlist Netlist { get; private set; } = builder?.Build(project.Model) ?? new([]);
    public int BuildCount { get; private set; } = 1;
    public void Rebuild() { if(builder is not null) { Netlist=builder.Build(project.Model); BuildCount++; } }
}
