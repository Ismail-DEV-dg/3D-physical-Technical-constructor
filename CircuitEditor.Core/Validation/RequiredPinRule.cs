namespace CircuitEditor.Core.Validation;
/// <summary>Обязательные сигнальные выводы, заданные библиотекой.</summary>
public sealed class RequiredPinRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c)
    {
        foreach(var component in c.Project.Circuit.Components)
            foreach(var pin in c.Catalog.Find(component.Definition.Id)?.RequiredPins ?? [])
                if(!c.Nets.Connected(component.Id,pin))
                    yield return ErcContext.Issue("REQUIRED_PIN",$"{component.Reference}: обязательный вывод {pin} не подключён.",[new(component.Id,pin)],
                        [c.Nets.Find(component.Id,pin)!.Id],ErcSeverity.Warning);
    }
}
