using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Core.Code;
/// <summary>Генератор ограниченных Arduino/ESP32 заготовок из JSON-шаблонов.</summary>
public sealed class ArduinoCodeGenerator(ICodeTemplateRepository repository) : ICodeGenerator
{
    /// <inheritdoc/>
    public CodeGenerationResult Generate(CircuitProject project, Netlist.Netlist nets, SketchDocument sketch)
    {
        var problems=new List<CodeProblem>();
        CodeGenerationResult Fail(string error,string code)=>new(false,sketch.Text,null,[new(error,Severity:"Error",Code:code)]);
        var loaded=repository.Load();if(loaded.Value is not { } catalog)return Fail(loaded.Error??"Нет шаблонов.","FILE_LIBRARY");
        var context=CodeCircuitContext.Create(project,nets,sketch,catalog,out var error);if(context is null)return Fail(error!,"CODE_BOARD_SELECTION");
        var includes=new SortedSet<string>(StringComparer.Ordinal);var globals=new List<string>();var setup=new List<string>();var loop=new List<string>();
        if(sketch.Board==BoardKind.Esp32&&CppTokenScanner.Scan(sketch.Text).GroupBy(t=>t.Line)
            .Any(g=>g.First().Text!="#"&&g.Any(t=>t.Text=="WiFi")))includes.Add("WiFi.h");
        string? i2cBus=null;
        foreach(var component in project.Circuit.Components.OrderBy(c=>c.Reference,StringComparer.Ordinal))
        {
            if(component.Id==context.Controller.Id)continue;
            var template=catalog.Templates.FirstOrDefault(t=>t.ComponentId==component.Definition.Id);
            if(template is null){globals.Add($"// {component.Reference}: {component.Definition.Name} — шаблон не поддерживается.");continue;}
            var values=new Dictionary<string,string> { ["REF"]=component.Reference,["ADDRESS"]=component.I2cAddress??"0x00",["SERVO_HEADER"]=context.Profile.ServoHeader };
            var definitions=new List<string>();var valid=true;
            foreach(var signal in template.Signals)
            {
                var pin=component.Definition.Id=="relay" ? CodeConnectionResolver.FindRelayControl(context,component)
                    : CodeConnectionResolver.Find(context,component.Id,signal.Pin,component.Definition.Id=="led");
                var reason=pin is null ? "нет единственного подключения к GPIO выбранной платы"
                    : signal.Direction is "output" or "bidirectional" or "i2c" && pin.IsInputOnly ? "GPIO допускает только ввод"
                    : template.RequiresPwm&&!pin.SupportsPwm ? "пин не поддерживает ШИМ для серво"
                    : signal.Direction=="analog"&&!pin.IsAnalog ? "нужен аналоговый вход" : null;
                if(reason is not null){problems.Add(new($"{component.Reference}, {signal.Pin}: {reason}.",Code:template.RequiresPwm?"CODE_SERVO_PWM":"CODE_PIN_CONNECTION"));valid=false;continue;}
                var macro=component.Reference.ToUpperInvariant()+"_"+signal.Suffix;
                values[signal.Suffix]=macro;definitions.Add($"#define {macro} {pin!.Code}");
                if(template.IsI2c)values[signal.Suffix+"_NUMBER"]=pin.Code;
            }
            if(!valid)
            {
                globals.Add($"// {component.Reference}: заготовка пропущена; проверьте подключение и драйвер.");
                if(template.Notes.Length>0)problems.Add(new(component.Reference+": "+template.Notes));continue;
            }
            if(component.Definition.Id=="button")
            {
                values["INPUT_MODE"]=CodeConnectionResolver.ButtonHasGround(context,component)?"INPUT_PULLUP":"INPUT";
                if(values["INPUT_MODE"]=="INPUT")problems.Add(new(component.Reference+": проверьте внешнюю подтяжку кнопки."));
            }
            string Render(string text)=>values.Aggregate(text,(value,p)=>value.Replace("{"+p.Key+"}",p.Value,StringComparison.Ordinal));
            if(template.IsI2c)
            {
                var sda=CodeConnectionResolver.Find(context,component.Id,"SDA")!;var scl=CodeConnectionResolver.Find(context,component.Id,"SCL")!;
                if(context.Profile.Kind!=BoardKind.Esp32 && (sda.Id is not ("SDA" or "A4") || scl.Id is not ("SCL" or "A5")))
                {problems.Add(new(component.Reference+": Wire на Uno/Nano требует SDA=A4 и SCL=A5."));globals.Add("// "+component.Reference+": неподдерживаемая распиновка I2C.");continue;}
                var bus=$"{sda.Number}:{scl.Number}";
                if(i2cBus is not null&&i2cBus!=bus){problems.Add(new(component.Reference+": шаблон поддерживает одну шину Wire; пины другой шины не подставлены."));continue;}
                if(i2cBus is null){setup.Add(context.Profile.Kind==BoardKind.Esp32?$"Wire.begin({sda.Code}, {scl.Code});":"Wire.begin();");i2cBus=bus;}
            }
            globals.AddRange(definitions);foreach(var include in template.Includes)includes.Add(Render(include));
            var standalone=template.StandaloneLoop.Length>0&&project.Circuit.Components.All(c=>c.Id==context.Controller.Id||c.Id==component.Id||c.Definition.Id=="resistor");
            globals.Add(Render(template.Declarations));setup.Add(Render(template.Setup));loop.Add(Render(standalone?template.StandaloneLoop:template.Loop));
            if(template.Notes.Length>0)problems.Add(new(component.Reference+": "+template.Notes));
        }
        var sections=new Dictionary<string,string> {
            ["globals"]=string.Join("\n",includes.Select(i=>"#include <"+i+">").Concat(globals)),
            ["setup"]=string.Join("\n",setup),["loop"]=string.Join("\n",loop)};
        try{return new(true,AutoRegionUpdater.Update(sketch.Text,sections),CodeCircuitSignature.Compute(project,nets,sketch),problems);}
        catch(ArgumentException e){return Fail(e.Message,"CODE_AUTO_MARKERS");}
    }
}
