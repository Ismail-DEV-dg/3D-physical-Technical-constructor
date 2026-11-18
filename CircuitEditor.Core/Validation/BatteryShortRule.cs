namespace CircuitEditor.Core.Validation;
/// <summary>Прямое замыкание батареи без нагрузки.</summary>
public sealed class BatteryShortRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c)
    {
        foreach(var battery in c.Project.Circuit.Components.Where(x=>x.Definition.Id=="battery"))
            if(c.Nets.Find(battery.Id,"+") is { } plus && plus==c.Nets.Find(battery.Id,"-"))
                yield return ErcContext.Issue("BATTERY_SHORT","Батарея замкнута накоротко без нагрузки.",plus.Pins,[plus.Id]);
    }
}
