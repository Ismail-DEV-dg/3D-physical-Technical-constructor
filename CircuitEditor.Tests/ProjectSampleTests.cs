using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Services;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Validation;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Scenarios;
using CircuitEditor.Simulation.Sketches;

namespace CircuitEditor.Tests;

public sealed class ProjectSampleTests
{
    public static IEnumerable<object[]> SketchExamples() =>
        new JsonProjectSampleRepository().Load().Select(sample => new object[] { sample.File });

    [Theory]
    [MemberData(nameof(SketchExamples))]
    public void EverySampleRunsItsSavedSketch(string file)
    {
        var samples = new JsonProjectSampleRepository();
        var sample = Assert.Single(samples.Load(), item => item.File == file);
        using var env = new Stage4TestEnvironment();
        var loaded = env.Repository.Load(samples.PathFor(sample));
        Assert.True(loaded.Success, loaded.Error);
        var document = loaded.Value!;
        Assert.Same(document.Sketch, document.Sketch.WithEntryPoints());
        var families = new JsonLogicFamilyRepository();
        var project = ProjectDocumentResolver.Resolve(document, env.Project.Library, env.Project.StandardFootprints,
            new JsonBreadboardRepository().Load().Value!, new ComponentInsertionService(), families.Load().Value);
        var library = new JsonElectricalLibraryRepository();
        var factory = new SimulationSessionFactory(new NetlistBuilder(library), new DcSolver(library), new JsonSimulationModelRepository(),
            new ScenarioRunner(new JsonScenarioRepository()), [new LedBehavior(), new I2cLcdBehavior(),
                new Dht22Behavior(), new ButtonBehavior(), new ChipBehavior(families, new ChipEvaluator())],
            sketches: new SketchRunner(new JsonCodeTemplateRepository()));
        var session = factory.CreateForSketch(project, document.Sketch);
        Assert.False(session.State.IsHalted, session.State.Failure);
        if (file == "uno-blink.circ")
        {
            var emptySketch = factory.CreateForSketch(project, document.Sketch with { Text = SketchDocument.StarterText });
            Assert.False(emptySketch.State.IsHalted, emptySketch.State.Failure);
        }
        var leds = project.Circuit.Components.Where(part => part.Definition.Id == "led").ToArray();
        var buttons = project.Circuit.Components.Where(part => part.Definition.Id == "button").ToArray();
        HashSet<string> patterns = [];
        List<double> brightness = [];
        while (session.State.Microseconds < 850_000)
        {
            var time = session.State.Microseconds;
            if (file == "uno-button.circ" && time == 100_000) Press(session, buttons[0], true);
            if (file == "uno-button.circ" && time == 300_000) Press(session, buttons[0], false);
            if (file == "decoder.circ" && time == 100_000) Press(session, buttons[0], true);
            if (file == "shift-register.circ" && time == 10_000) Press(session, buttons[0], true);
            if (file == "shift-register.circ" && time == 110_000) Press(session, buttons[0], false);
            var frame = session.Step();
            Assert.False(frame.IsHalted, frame.Failure);
            Assert.DoesNotContain(frame.Diagnostics, diagnostic => diagnostic.IsError);
            patterns.Add(string.Concat(leds.Select(led => frame.Components[led.Id].Intensity > 0 ? '1' : '0')));
            if (file == "uno-pwm.circ") brightness.Add(frame.Components[leds[0].Id].Intensity);
        }
        switch (file)
        {
            case "uno-blink.circ" or "uno-button.circ" or "uno-millis.circ":
                Assert.Contains("0", patterns);
                Assert.Contains("1", patterns);
                break;
            case "uno-pwm.circ":
                Assert.True(brightness.Min() > 0);
                Assert.True(brightness.Max() > brightness.Min());
                break;
            case "uno-two-leds.circ":
                Assert.Equal(new[] { "01", "10" }, patterns.Order());
                break;
            case "i2c-lcd.circ":
                Assert.Contains(session.State.Components.Values, part => part.Text == "Circuit Editor");
                break;
            case "esp32-dht22.circ":
                var sensor = project.Circuit.Components.Single(part => part.Definition.Id == "dht22");
                string Reading(ComponentInput key) => sensor.Inputs.Values[key].ToString("G", System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(new[] { "DHT22: Temperature", Reading(ComponentInput.Temperature),
                    "DHT22: Humidity", Reading(ComponentInput.Humidity) }, session.State.Serial.Select(line => line.Text));
                break;
            case "decoder.circ":
                Assert.Contains("1000", patterns);
                Assert.Contains("0100", patterns);
                break;
            case "shift-register.circ":
                Assert.Contains("10000000", patterns);
                break;
            default: throw new InvalidOperationException("У примера отсутствует проверка результата.");
        }
    }

    [Theory]
    [InlineData("uno-blink.circ")]
    [InlineData("i2c-lcd.circ")]
    [InlineData("esp32-dht22.circ")]
    [InlineData("decoder.circ")]
    [InlineData("shift-register.circ")]
    public void SampleOpensPassesErcAndShowsItsBehavior(string file)
    {
        var samples = new JsonProjectSampleRepository();
        var sample = Assert.Single(samples.Load(), item => item.File == file);
        Assert.NotEmpty(sample.Description);
        using var env = new Stage4TestEnvironment();
        var data = env.Repository.Load(samples.PathFor(sample));
        Assert.True(data.Success, data.Error);
        var document = data.Value!;
        Assert.Equal(4, document.SchemaVersion);
        Assert.NotEmpty(document.Sketch.Text);
        var families = new JsonLogicFamilyRepository();
        var project = ProjectDocumentResolver.Resolve(document, env.Project.Library, env.Project.StandardFootprints,
            new JsonBreadboardRepository().Load().Value!, new ComponentInsertionService(), families.Load().Value);
        var library = new JsonElectricalLibraryRepository();
        var nets = new NetlistBuilder(library);
        IErcRule[] rules = [new SupplyShortRule(), new SourceConflictRule(), new BatteryShortRule(),
            new EspOvervoltageRule(), new DisconnectedSupplyRule(), new ChipSupplyRule(), new RequiredPinRule(),
            new I2cAddressRule(), new LedResistorRule(), new LedShortRule(), new PinDirectionRule()];
        var errors = new ErcService(library, rules).Check(project, nets.Build(project))
            .Where(issue => issue.Severity == ErcSeverity.Error);
        Assert.Empty(errors);

        var factory = new SimulationSessionFactory(nets, new DcSolver(library), new JsonSimulationModelRepository(),
            new ScenarioRunner(new JsonScenarioRepository()), [new LedBehavior(), new I2cLcdBehavior(),
                new Dht22Behavior(), new ButtonBehavior(), new ChipBehavior(families, new ChipEvaluator())]);
        var session = factory.Create(project, Assert.IsType<ScenarioConfiguration>(project.SimulationSetup.Scenario));
        var leds = project.Circuit.Components.Where(part => part.Definition.Id == "led").ToArray();
        var buttons = project.Circuit.Components.Where(part => part.Definition.Id == "button").ToArray();
        List<string> patterns = [];
        while (session.State.Microseconds < 1_000_000)
        {
            var t = session.State.Microseconds;
            if (file == "shift-register.circ" && t == 0) Press(session, buttons[0], true);
            if (file == "shift-register.circ" && t == 110_000) Press(session, buttons[0], false);
            if (file == "decoder.circ" && t == 250_000) Press(session, buttons[0], true);
            if (file == "decoder.circ" && t == 500_000) { Press(session, buttons[0], false); Press(session, buttons[1], true); }
            if (file == "decoder.circ" && t == 750_000) Press(session, buttons[0], true);
            var state = session.Step();
            Assert.False(state.IsHalted, state.Failure);
            Assert.DoesNotContain(state.Diagnostics, diagnostic => diagnostic.IsError);
            if (leds.Length > 0)
                patterns.Add(string.Concat(leds.Select(led => state.Components[led.Id].Intensity > 0 ? '1' : '0')));
        }

        switch (file)
        {
            case "uno-blink.circ":
                Assert.Contains("0", patterns);
                Assert.Contains("1", patterns);
                break;
            case "i2c-lcd.circ":
                Assert.Contains(session.State.Components.Values, item => item.Active && item.Text == "Circuit Editor");
                break;
            case "esp32-dht22.circ":
                Assert.Contains(session.State.Serial, line => line.Text.Contains("DHT22") && line.Text.Contains("Temperature") && line.Text.Contains("Humidity"));
                break;
            case "decoder.circ":
                Assert.Equal(new[] { "1000", "0100", "0010", "0001" }, patterns.Where(pattern => pattern.Contains('1')).Distinct());
                break;
            case "shift-register.circ":
                Assert.Contains("10000000", patterns);
                Assert.Contains("11110000", patterns);
                Assert.True(patterns.Distinct().Count() >= 6);
                break;
        }
    }

    private static void Press(SimulationSession session, Component button, bool pressed) =>
        Assert.True(session.SetInput(button.Id, ComponentInput.ButtonPressed, pressed ? 1 : 0));

    [Fact]
    public void OpeningExampleMarksCopyDirtyAndProtectsOriginal()
    {
        using var env = new Stage4TestEnvironment();
        var catalog = new JsonProjectSampleRepository();
        var session = new App.ViewModels.ProjectSessionViewModel(env.Project, env.Mockup, env.Schematic,
            env.Navigation, env.Repository, env, env, env, new JsonBreadboardRepository(), new ComponentInsertionService(),
            new JsonLogicFamilyRepository(), samples: catalog);
        var example = Assert.Single(session.Samples, item => item.Sample.File == "uno-blink.circ");
        example.OpenCommand.Execute(example.Sample);
        Assert.Empty(env.Errors);
        Assert.True(env.Project.IsDirty);
        Assert.StartsWith("* Новый проект", session.Title);
        env.Target = catalog.PathFor(example.Sample);
        session.SaveCommand.Execute(null);
        Assert.Contains("Пример защищён", Assert.Single(env.Errors));
        Assert.True(env.Project.IsDirty);
    }
}
