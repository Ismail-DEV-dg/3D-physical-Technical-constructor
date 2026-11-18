using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
namespace CircuitEditor.Core.Validation;
/// <summary>Общий контекст правил; библиотека не изменяется.</summary>
public sealed class ErcContext(CircuitProject project, Netlist.Netlist nets, ElectricalCatalog catalog)
{
    /// <summary>Проверяемая модель.</summary>
    public CircuitProject Project => project;
    /// <summary>Снимок узлов.</summary>
    public Netlist.Netlist Nets => nets;
    /// <summary>Данные электрических моделей.</summary>
    public ElectricalCatalog Catalog => catalog;
    /// <summary>Источники с известным напряжением; VIN без задания напряжения не угадывается.</summary>
    public IReadOnlyList<PowerSource> Sources => project.Circuit.Components.SelectMany(c=>(catalog.Find(c.Definition.Id)?.Sources ?? [])
        .Select(s=>new PowerSource(c.Id,new(c.Id,s.Pin),new(c.Id,s.Ground),s.FromBatteryProperty ? c.Inputs.Values[ComponentInput.BatteryVoltage] : s.Voltage))).ToArray();
    /// <summary>Формирует адресуемое сообщение.</summary>
    public static ErcIssue Issue(string code,string message,IEnumerable<PinReference> pins,IEnumerable<string> nodes,ErcSeverity severity=ErcSeverity.Error)
    {
        var p=pins.Distinct().ToArray(); return new(severity,code,message,p.Select(x=>x.ComponentId).Distinct().ToArray(),p,nodes.Distinct().ToArray());
    }
    /// <summary>Находит электрический тип вывода.</summary>
    public Pin Pin(PinReference pin) => project.Circuit.Components.Single(c=>c.Id==pin.ComponentId).Definition.Pins.Single(p=>p.Id==pin.PinId);
}
