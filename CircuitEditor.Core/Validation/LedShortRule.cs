using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Validation;
/// <summary>Обнаруживает соединение обоих выводов LED одним электрическим узлом.</summary>
public sealed class LedShortRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext context)
    {
        foreach(var led in context.Project.Circuit.Components.Where(c=>c.Definition.Id=="led"))
        {
            var a=context.Nets.Find(led.Id,"A");var k=context.Nets.Find(led.Id,"K");
            if(a is not null&&a.Id==k?.Id)yield return ErcContext.Issue("LED_SHORT",$"{led.Reference}: анод и катод соединены одной группой отверстий или перемычкой. Разместите ножки в разных электрических узлах.",
                [new PinReference(led.Id,"A"),new PinReference(led.Id,"K")],[a.Id],ErcSeverity.Warning);
        }
    }
}
