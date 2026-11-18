using System.Globalization;
using System.Text;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Core.Export;
/// <summary>Группирует одинаковые детали с учётом параметров экземпляра.</summary>
public sealed class BomExportService(IExportRepository repository) : IBomExportService
{
    /// <inheritdoc/>
    public byte[] Create(CircuitProject project)
    {
        static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
        var rows = new StringBuilder("Позиция;Обозначение;Тип;Номинал/параметры;Количество\r\n");
        var index = 0;
        foreach (var group in project.Circuit.Components.GroupBy(c => (c.Definition.Id, c.Value, Color: c.Definition.Id == "led" ? c.Color : "",
            Kind: c.Definition.Id == "capacitor" ? c.CapacitorKind.ToString() : "", c.I2cAddress,
            Inputs: string.Join(", ", c.Inputs.Values.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value.ToString(CultureInfo.InvariantCulture)}")),
            Electrical: string.Join(", ",c.Electrical.Parameters.OrderBy(p=>p.Key).Select(p=>$"{p.Key}={p.Value.ToString(CultureInfo.InvariantCulture)}")),
            Angle: c.Definition.Id == "potentiometer" ? c.PotentiometerAngle : 0)))
        {
            var c = group.First();
            var parameters = string.Join(" · ", new[] { c.Value, group.Key.Color, group.Key.Kind, c.I2cAddress, group.Key.Inputs, group.Key.Electrical,
                group.Key.Angle == 0 ? null : group.Key.Angle.ToString(CultureInfo.InvariantCulture) }.Where(s => !string.IsNullOrEmpty(s)));
            rows.Append(++index).Append(';').Append(Quote(string.Join(", ", group.Select(x => x.Reference)))).Append(';')
                .Append(Quote(c.Definition.Name)).Append(';').Append(Quote(parameters)).Append(';').Append(group.Count()).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(rows.ToString())).ToArray();
    }
    /// <inheritdoc/>
    public string? Export(string path, CircuitProject project) => repository.Write(path, Create(project));
}
