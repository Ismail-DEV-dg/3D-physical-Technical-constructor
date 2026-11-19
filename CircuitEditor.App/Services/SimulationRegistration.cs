using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Scenarios;
using Microsoft.Extensions.DependencyInjection;

namespace CircuitEditor.App.Services;

internal static class SimulationRegistration
{
    internal static void AddSimulation(this IServiceCollection services)
    {
        services.AddSingleton<ISimulationModelRepository, JsonSimulationModelRepository>();
        services.AddSingleton<IScenarioRepository, JsonScenarioRepository>();
        services.AddSingleton<IScenarioRunner, ScenarioRunner>();
        services.AddSingleton<IComponentBehavior, LedBehavior>();
        services.AddSingleton<IComponentBehavior, RelayBehavior>();
        services.AddSingleton<IComponentBehavior, BuzzerBehavior>();
        services.AddSingleton<IComponentBehavior, ServoBehavior>();
        services.AddSingleton<IComponentBehavior, DisplayBehavior>();
        services.AddSingleton<IComponentBehavior, SevenSegmentBehavior>();
        services.AddSingleton<IComponentBehavior, DiodeBehavior>();
        services.AddSingleton<IComponentBehavior, NpnBehavior>();
        services.AddSingleton<IComponentBehavior, SensorBehavior>();
        services.AddSingleton<IComponentBehavior, PirBehavior>();
        services.AddSingleton<IComponentBehavior, UltrasonicBehavior>();
        services.AddSingleton<ISimulationSessionFactory, SimulationSessionFactory>();
        services.AddSingleton<ISimulationPlayback, SimulationPlayback>();
        services.AddSingleton<ISimulationUiPulse, SimulationUiPulse>();
        services.AddSingleton<SimulationViewModel>();
    }
}
