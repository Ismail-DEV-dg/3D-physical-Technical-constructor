using CircuitEditor.App.Services;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace CircuitEditor.Tests;

public sealed class SimulationCompositionTests
{
    [Fact]
    public void CompositionCreatesIndependentClocksAndAllRegisteredDeviceModels()
    {
        using var design = new Stage4TestEnvironment();
        foreach (var definition in design.Project.Library) design.Project.Add(definition);
        var registrations = new ServiceCollection();
        registrations.AddSingleton(design.Project);
        registrations.AddSingleton<IElectricalLibraryRepository, JsonElectricalLibraryRepository>();
        registrations.AddSingleton<INetlistBuilder, NetlistBuilder>();
        registrations.AddSingleton<IDcSolver, DcSolver>();
        registrations.AddSimulation();
        using var provider = registrations.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var factory = provider.GetRequiredService<ISimulationSessionFactory>();
        var first = factory.Create(design.Project.Model);
        var second = factory.Create(design.Project.Model);
        Assert.Equal(1000, first.Step().Microseconds);
        Assert.False(first.State.IsHalted);
        Assert.Equal(0, second.State.Microseconds);
        Assert.Equal(2, first.State.Diagnostics.Count(issue => issue.Code == "SIM_UNSUPPORTED"));
    }
}
