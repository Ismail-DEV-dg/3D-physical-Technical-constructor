using CircuitEditor.Core.Models;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Resistance;
using CircuitEditor.Core.Validation;
namespace CircuitEditor.Core.Electronics;
/// <summary>Преобразует поддерживаемые модели в ветви; остальные явно исключает.</summary>
public static class DcCircuitAssembler
{
    /// <summary>Собирает линейные и кусочно-линейные ветви из снимка проекта.</summary>
    public static DcAssembly Assemble(CircuitProject project,Netlist.Netlist nets,ElectricalCatalog catalog,
        IReadOnlySet<Guid>? modeledComponents = null)
    {
        var branches=new List<DcBranch>();var unsupported=new List<Guid>();var messages=new List<string>();var invalid=false;
        foreach(var c in project.Circuit.Components)
        {
            var definition=catalog.Find(c.Definition.Id);var model=definition?.Model??"unsupported";
            string? Node(string pin)=>nets.Find(c.Id,pin)?.Id;
            void Add(string name,string a,string b,string kind,double value,double vf=0,double limit=0)
            { if(Node(a) is { } x && Node(b) is { } y) branches.Add(new(c.Id,c.Reference+name,x,y,kind,value,vf,limit)); }
            double Parameter(string key,double fallback)=>c.Electrical.Parameters.GetValueOrDefault(key,fallback);
            if(model is "resistor" or "potentiometer")
            {
                if(!ResistanceFormatter.TryParse(c.Value,out var nominal)){messages.Add(c.Reference+": некорректный номинал сопротивления.");invalid=true;continue;}
                if(model=="resistor")Add("","1","2","R",(double)nominal);
                else
                {
                    var fraction=Physical.PotentiometerPosition.ToPercent(c.PotentiometerAngle)/100;
                    Add(".A-W","A","W",fraction==0?"V":"R",(double)nominal*fraction);
                    Add(".W-B","W","B",fraction==1?"V":"R",(double)nominal*(1-fraction));
                }
            }
            else if(model=="led")
            {var profile=ElectricalParameters.Led(c,catalog);Add("","A","K","LED",profile.DynamicResistance,profile.Vf,profile.MaximumCurrent);}
            else if(model=="rgb-led")
            {
                foreach(var channel in definition!.ChannelProfiles!.Keys)
                {
                    var profile=ElectricalParameters.Led(c,catalog,channel);var anode=c.Inputs.Values[ComponentInput.RgbCommon]==1;
                    Add("."+channel,anode?"COM":channel,anode?channel:"COM","LED",profile.DynamicResistance,profile.Vf,profile.MaximumCurrent);
                }
            }
            else if(model=="ldr")
            {
                var illumination=c.Inputs.Values[ComponentInput.Illuminance]/100;
                var dark=Parameter("DarkResistance",definition!.DarkResistance!.Value);var light=Parameter("LightResistance",definition.LightResistance!.Value);
                Add("","1","2","R",Math.Exp(Math.Log(dark)*(1-illumination)+Math.Log(light)*illumination));
            }
            else if(model=="ntc")
            {
                const double CelsiusZero=273.15;const double ReferenceCelsius=25;
                var r25=Parameter("R25",definition!.R25!.Value);var beta=Parameter("Beta",definition.Beta!.Value);
                var resistance=r25*Math.Exp(beta*(1/(c.Inputs.Values[ComponentInput.Temperature]+CelsiusZero)-1/(ReferenceCelsius+CelsiusZero)));
                Add("","1","2","R",resistance);
            }
            else if(model=="switch")messages.Add(c.Reference+": идеальные замкнутые контакты входят в узлы; отдельный ток контакта не определяется после объединения.");
            else if(model!="source" && modeledComponents?.Contains(c.Id) != true){unsupported.Add(c.Id);messages.Add(c.Reference+" · "+c.Definition.Name+": не поддерживается и исключён из DC-расчёта.");}
        }
        foreach(var s in new ErcContext(project,nets,catalog).Sources)
            if(nets.Find(s.ComponentId,s.Positive.PinId) is { } a && nets.Find(s.ComponentId,s.Negative.PinId) is { } b)
                branches.Add(new(s.ComponentId,project.Circuit.Components.Single(c=>c.Id==s.ComponentId).Reference+"."+s.Positive.PinId,a.Id,b.Id,"V",s.Voltage));
        return new(branches,unsupported,messages,invalid);
    }
}
