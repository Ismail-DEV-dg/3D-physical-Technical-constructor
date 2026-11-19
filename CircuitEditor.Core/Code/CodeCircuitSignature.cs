using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Code;
/// <summary>Подпись электрических данных; координаты, цвет и изгибы перемычек исключены.</summary>
public static class CodeCircuitSignature
{
    /// <summary>Создаёт стабильную подпись для проверки актуальности генерации.</summary>
    public static string Compute(CircuitProject project, Netlist.Netlist nets, SketchDocument sketch)
    {
        var data=new {sketch.Board,sketch.ControllerComponentId,
            Components=project.Circuit.Components.OrderBy(c=>c.Id).Select(c=>new {c.Id,c.Reference,Definition=c.Definition.Id,c.Value,c.I2cAddress,
                Parameters=c.Electrical.Parameters.OrderBy(p=>p.Key),Roles=c.Electrical.PinRoles.OrderBy(p=>p.Key)}),
            Nets=nets.Nets.Where(n=>n.Pins.Count>0).Select(n=>string.Join(";",n.Pins.Select(p=>$"{p.ComponentId}:{p.PinId}").Order(StringComparer.Ordinal))).Order(StringComparer.Ordinal)};
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data))));
    }
}
