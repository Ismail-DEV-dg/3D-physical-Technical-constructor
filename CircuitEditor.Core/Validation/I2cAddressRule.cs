using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Validation;
/// <summary>Адрес конфликтует только при общей SDA и общей SCL.</summary>
public sealed class I2cAddressRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c)
    {
        var modules=c.Project.Circuit.Components.Where(x=>x.I2cAddress is not null).ToArray();
        for(var i=0;i<modules.Length;i++) for(var j=i+1;j<modules.Length;j++)
        {
            var a=modules[i];var b=modules[j]; if(a.I2cAddress!=b.I2cAddress) continue;
            string Pin(Component m,PinType type)=>m.Definition.Pins.First(p=>p.Type==type).Id;
            var sda=c.Nets.Find(a.Id,Pin(a,PinType.I2C_SDA));var scl=c.Nets.Find(a.Id,Pin(a,PinType.I2C_SCL));
            if(sda==c.Nets.Find(b.Id,Pin(b,PinType.I2C_SDA)) && scl==c.Nets.Find(b.Id,Pin(b,PinType.I2C_SCL)))
                yield return ErcContext.Issue("I2C_CONFLICT",$"{a.Reference}/{b.Reference}: одинаковый I2C-адрес {a.I2cAddress} на общей шине.",
                    [new(a.Id,Pin(a,PinType.I2C_SDA)),new(b.Id,Pin(b,PinType.I2C_SDA))],[sda!.Id,scl!.Id]);
        }
    }
}
