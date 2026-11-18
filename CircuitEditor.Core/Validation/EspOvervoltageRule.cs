namespace CircuitEditor.Core.Validation;
/// <summary>Известное напряжение выше предела GPIO ESP32.</summary>
public sealed class EspOvervoltageRule : IErcRule
{
    /// <summary>Предел статической проверки GPIO, В.</summary>
    public const double MaximumGpioVoltage=3.6;
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c)
    {
        foreach(var source in c.Sources.Where(s=>s.Voltage>MaximumGpioVoltage))
        {
            var net=c.Nets.Find(source.ComponentId,source.Positive.PinId); if(net is null) continue;
            var pins=net.Pins.Where(p=>p.PinId.StartsWith("GPIO",StringComparison.Ordinal)
                && c.Project.Circuit.Components.Any(x=>x.Id==p.ComponentId && x.Definition.Id=="esp32-devkit")).ToArray();
            if(pins.Length>0) yield return ErcContext.Issue("ESP_OVERVOLTAGE",$"На GPIO ESP32 подано {source.Voltage:G} В, допустимо до {MaximumGpioVoltage:G} В.",pins.Append(source.Positive),[net.Id]);
        }
    }
}
