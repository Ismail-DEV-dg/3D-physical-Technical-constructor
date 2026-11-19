using System.Globalization;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Gpio;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Scenarios;

public sealed class ScenarioRunner(IScenarioRepository repository) : IScenarioRunner
{
    private const long InputPollMicroseconds = 1_000;
    private const int MaximumTextLength = 512;
    private const double MaximumPeriodMs = 3_600_000;

    public SimulationDiagnostic? Start(ScenarioConfiguration configuration, SimulationCircuit circuit, BehaviorContext context,
        ISimulationEngine engine, ISimulationClock clock, Action<string> serial)
    {
        var template = repository.Load().Value?.SingleOrDefault(t => t.Id == configuration.TemplateId);
        if (template is null) return Invalid("Шаблон сценария отсутствует.");
        foreach (var (name, role) in template.Pins)
        {
            if (!configuration.Pins.TryGetValue(name, out var binding)) return Invalid($"Выберите вывод {name}.");
            if (!circuit.Controllers.TryGetValue(binding.ComponentId, out var board) ||
                board.Pins.FirstOrDefault(p => p.Id == binding.PinId) is not { } pin ||
                role is "output" or "pwm" && pin.IsInputOnly ||
                role == "pwm" && pin.Type != PinType.PWM && !pin.Capabilities.Contains(PinType.PWM) ||
                role == "analog" && pin.Type != PinType.AnalogIn && !pin.Capabilities.Contains(PinType.AnalogIn))
                return Invalid($"Вывод {name} ({binding.PinId}) отсутствует или не поддерживает выбранный режим.", binding);
        }

        var target = configuration.TargetId is Guid id ? circuit.Project.Circuit.Components.SingleOrDefault(c => c.Id == id) : null;
        if (template.Target == "display" && target?.Definition.Id is not ("lcd1602" or "lcd1602-i2c" or "oled-ssd1306") ||
            configuration.TargetId is not null && (target is null || template.Target == "sensor" &&
                target.Definition.Id is not ("dht11" or "dht22" or "pir" or "hc-sr04" or "thermistor" or "photoresistor")))
            return Invalid("Получатель сценария удалён или имеет другой тип.");

        double Number(string key) => configuration.Numbers.GetValueOrDefault(key, template.Numbers.GetValueOrDefault(key));
        var periodMs = Number("periodMs");
        var duty = Number("duty");
        var minimum = Number("minimumAngle");
        var maximum = Number("maximumAngle");
        var text = configuration.Text ?? template.Text ?? "";
        if (template.Numbers.ContainsKey("periodMs") && (!double.IsFinite(periodMs) || periodMs is < 1 or > MaximumPeriodMs) ||
            template.Id == "blink" && (!double.IsFinite(duty) || duty is < 0 or > 1) ||
            template.Id == "servo-sweep" && (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum < 0 || maximum > 180 || minimum > maximum) ||
            template.Id == "button-led" && Number("activeLow") is not (0 or 1) ||
            text.Length > MaximumTextLength)
            return Invalid("Проверьте период, скважность, угол и длину текста сценария.");

        var period = (long)Math.Round(periodMs * 1000);
        void Set(string key, GpioState state) => circuit.TrySetPin(configuration.Pins[key], state);
        DigitalLevel Read(string key) => circuit.ReadDigital(configuration.Pins[key], clock.Microseconds);
        void Repeat(long interval, Action tick)
        {
            void Next()
            {
                tick();
                engine.Schedule(clock.Microseconds + interval, Next);
            }
            engine.Schedule(clock.Microseconds, Next);
        }

        switch (template.Id)
        {
            case "blink":
                var highLength = (long)Math.Round(period * duty);
                Repeat(period, () =>
                {
                    Set("LED_PIN", new(GpioMode.Output, highLength > 0));
                    if (highLength > 0 && highLength < period)
                        engine.Schedule(clock.Microseconds + highLength, () => Set("LED_PIN", new(GpioMode.Output)));
                });
                break;
            case "button-led":
                var activeLow = Number("activeLow") == 1;
                Set("BUTTON_PIN", new(activeLow ? GpioMode.InputPullup : GpioMode.Input));
                Repeat(InputPollMicroseconds, () => Set("LED_PIN", new(GpioMode.Output,
                    Read("BUTTON_PIN") == (activeLow ? DigitalLevel.Low : DigitalLevel.High))));
                break;
            case "pot-pwm":
                Repeat(InputPollMicroseconds, () =>
                {
                    var input = configuration.Pins["POT_PIN"];
                    var board = circuit.Controllers[input.ComponentId];
                    var adc = board.ReadAdc(input.PinId, circuit.Voltage(input)) ?? 0;
                    Set("LED_PIN", new(GpioMode.Pwm, Duty: (double)adc / ((1 << board.Profile.AdcBits) - 1)));
                });
                break;
            case "servo-sweep":
                var towardsMaximum = false;
                Repeat(period, () =>
                {
                    towardsMaximum = !towardsMaximum;
                    var angle = towardsMaximum ? maximum : minimum;
                    var output = configuration.Pins["SERVO_PIN"];
                    var servo = circuit.Nets.Find(output.ComponentId, output.PinId)?.Pins
                        .Select(p => circuit.Project.Circuit.Components.First(c => c.Id == p.ComponentId))
                        .FirstOrDefault(c => c.Definition.Id == "servo");
                    if (servo is null) return;
                    var frame = (long)context.Parameter(servo, "frameUs");
                    var lower = context.Parameter(servo, "minimumPulseUs");
                    var upper = context.Parameter(servo, "maximumPulseUs");
                    Set("SERVO_PIN", new(GpioMode.Pwm, Duty: (lower + (upper - lower) * angle / 180) / frame,
                        PeriodMicroseconds: frame));
                });
                break;
            case "display":
                context.SetText(target!.Id, text);
                break;
            case "serial":
                Repeat(period, () =>
                {
                    var values = target is null ? [] : target.Inputs.Values.OrderBy(p => p.Key)
                        .Select(p => $"{p.Key}={p.Value.ToString("G", CultureInfo.InvariantCulture)}").ToArray();
                    serial(values.Length == 0 ? text : $"{text} {string.Join(", ", values)}");
                });
                break;
            default:
                return Invalid("Обработчик сценария отсутствует.");
        }
        return null;
    }

    private static SimulationDiagnostic Invalid(string reason, PinReference? pin = null) =>
        new("SIM_BINDING", $"Привязка недействительна. {reason}", 0, pin?.ComponentId, pin?.PinId, IsError: true);
}
