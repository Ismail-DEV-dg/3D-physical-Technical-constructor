using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Instruments;
using CircuitEditor.Simulation.Scenarios;
using CircuitEditor.Simulation.Sketches;
using CircuitEditor.Simulation.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CircuitEditor.App.Services;

internal static class SimulationRegistration
{
    internal static void AddSimulation(this IServiceCollection services)
    {
        services.AddSingleton<ISimulationModelRepository, JsonSimulationModelRepository>();
        services.AddSingleton<ILogicFamilyRepository, JsonLogicFamilyRepository>();
        services.AddSingleton<CircuitEditor.Core.Chips.IChipEvaluator, CircuitEditor.Core.Chips.ChipEvaluator>();
        services.AddSingleton<IScenarioRepository, JsonScenarioRepository>();
        services.AddSingleton<IScenarioRunner, ScenarioRunner>();
        services.TryAddSingleton<ICodeTemplateRepository, JsonCodeTemplateRepository>();
        services.AddSingleton<ISketchRunner, SketchRunner>();
        services.AddSingleton<IComponentBehavior, LedBehavior>();
        services.AddSingleton<IComponentBehavior, ChipBehavior>();
        services.AddSingleton<IComponentBehavior, RgbLedBehavior>();
        services.AddSingleton<IComponentBehavior, RelayBehavior>();
        services.AddSingleton<IComponentBehavior, BuzzerBehavior>();
        services.AddSingleton<IComponentBehavior, ServoBehavior>();
        services.AddSingleton<IComponentBehavior, LcdBehavior>();
        services.AddSingleton<IComponentBehavior, I2cLcdBehavior>();
        services.AddSingleton<IComponentBehavior, OledBehavior>();
        services.AddSingleton<IComponentBehavior, SevenSegmentBehavior>();
        services.AddSingleton<IComponentBehavior, DiodeBehavior>();
        services.AddSingleton<IComponentBehavior, NpnBehavior>();
        services.AddSingleton<IComponentBehavior, Dht11Behavior>();
        services.AddSingleton<IComponentBehavior, Dht22Behavior>();
        services.AddSingleton<IComponentBehavior, LdrBehavior>();
        services.AddSingleton<IComponentBehavior, NtcBehavior>();
        services.AddSingleton<IComponentBehavior, PotentiometerBehavior>();
        services.AddSingleton<IComponentBehavior, ButtonBehavior>();
        services.AddSingleton<IComponentBehavior, SwitchBehavior>();
        services.AddSingleton<IComponentBehavior, PirBehavior>();
        services.AddSingleton<IComponentBehavior, UltrasonicBehavior>();
        services.AddSingleton<ISimulationDiagnostics, SimulationDiagnostics>();
        services.AddSingleton<IInstrument, Multimeter>();
        services.AddSingleton<IInstrument, Oscilloscope>();
        services.AddSingleton<IInstrument, LogicAnalyzer>();
        services.AddSingleton<IChipTestBenchFactory, ChipTestBenchFactory>();
        services.AddTransient<ISimulationClock, SimulationClock>();
        services.AddTransient<ISimulationEngine, SimulationEngine>();
        services.AddSingleton<Func<ISimulationEngine>>(provider => provider.GetRequiredService<ISimulationEngine>);
        services.AddSingleton<ISimulationSessionFactory, SimulationSessionFactory>();
        services.AddSingleton<ISimulationPlayback, SimulationPlayback>();
        services.AddSingleton<ISimulationUiPulse, SimulationUiPulse>();
        services.AddSingleton<SimulationViewModel>();
    }
}
