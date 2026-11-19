using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Validation;

/// <summary>Проверяет явные назначения направления в проекте.</summary>
public sealed class PinDirectionRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext context)
    {
        var parts = context.Project.Circuit.Components.ToDictionary(c => c.Id);
        foreach (var net in context.Nets.Nets)
        {
            var outputs = net.Pins.Where(p => parts[p.ComponentId].Electrical.Role(p.PinId) == PinRole.Output).ToArray();
            foreach (var pin in outputs.Where(p => context.Pin(p).IsInputOnly))
                yield return ErcContext.Issue("PIN_INPUT_ONLY", $"{parts[pin.ComponentId].Reference}.{pin.PinId}: вывод предназначен только для входа.", [pin], [net.Id]);
            if (outputs.Length > 1)
                yield return ErcContext.Issue("PIN_MULTIPLE_OUTPUTS",
                    $"{net.Name}: на одном узле несколько выводов с ролью «Выход». Проверьте уровни при симуляции.",
                    outputs, [net.Id], ErcSeverity.Warning);
        }
    }
}
