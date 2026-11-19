using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Core.Code;
/// <summary>Сверяет уверенно распознанные пины с выбранной платой и известными ролями.</summary>
public sealed class ArduinoCodeAnalyzer(ICodeCompletionRepository completion,ICodeTemplateRepository templates) : ICodeAnalyzer
{
    /// <inheritdoc/>
    public IReadOnlyList<CodeProblem> Analyze(CircuitProject project,Netlist.Netlist nets,SketchDocument sketch)
    {
        var catalog=templates.Load();var api=completion.Load();
        if(catalog.Value is null||api.Value is null)return [new(catalog.Error??api.Error??"Нет данных анализатора.",Severity:"Info")];
        var profile=catalog.Value.Boards.Single(b=>b.Kind==sketch.Board);
        var uses=PinUsageCollector.Collect(sketch.Text,profile,api.Value);var problems=new List<CodeProblem>();
        foreach(var use in uses.Where(u=>u.Writes&&u.Pin.IsInputOnly))
            problems.Add(new($"{use.Pin.Id}: вывод допускает только ввод; {use.Function} пытается использовать его как выход.",use.Line,use.Column,Code:"CODE_INPUT_ONLY"));
        var context=CodeCircuitContext.Create(project,nets,sketch,catalog.Value,out var error);
        if(context is null){if(uses.Count>0)problems.Add(new(error!,Severity:"Info",Code:"CODE_BOARD_SELECTION"));return problems;}
        var connected=profile.Pins.Where(p=>p.IsExposed&&nets.Connected(context.Controller.Id,p.Id)).ToArray();
        foreach(var use in uses.DistinctBy(u=>(u.Pin.Number,u.Line,u.Writes)))
        {
            if(!connected.Any(p=>p.Number==use.Pin.Number))problems.Add(new($"{use.Pin.Id} используется в коде, но не подключён к компоненту в схеме.",use.Line,use.Column,Code:"CODE_PIN_CONNECTION"));
            if(use.Writes&&!use.Pin.IsInputOnly&&context.Controller.Electrical.Role(use.Pin.Id)==PinRole.Input)
                problems.Add(new($"{use.Pin.Id}: запись при известной роли Input в схеме.",use.Line,use.Column,Code:"CODE_PIN_DIRECTION"));
        }
        foreach(var pin in connected.DistinctBy(p=>p.Number).Where(p=>!uses.Any(u=>u.Pin.Number==p.Number)))
            problems.Add(new($"{pin.Id} подключён в схеме, но не найден в поддерживаемых вызовах API. Проверьте пользовательские функции и макросы.",Severity:"Info",Code:"CODE_PIN_UNUSED"));
        return problems;
    }
}
