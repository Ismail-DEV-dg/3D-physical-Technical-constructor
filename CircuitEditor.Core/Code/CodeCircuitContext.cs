using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Code;
/// <summary>Определяет единственный контроллер без угадывания при нескольких платах.</summary>
public sealed record CodeCircuitContext(CircuitProject Project, Netlist.Netlist Nets, CodeBoardProfile Profile, Component Controller)
{
    /// <summary>Возвращает выбранный контроллер или понятную причину неоднозначности.</summary>
    public static CodeCircuitContext? Create(CircuitProject project, Netlist.Netlist nets, SketchDocument sketch, CodeCatalog catalog, out string? error)
    {
        error=null;var profile=catalog.Boards.Single(b=>b.Kind==sketch.Board);
        var boards=project.Circuit.Components.Where(c=>c.Definition.Id==profile.ComponentId
            && (sketch.ControllerComponentId is null || c.Id==sketch.ControllerComponentId)).ToArray();
        if(boards.Length!=1){error="Выберите единственный экземпляр платы "+sketch.Board+" для генерации и проверки подключений.";return null;}
        return new(project,nets,profile,boards[0]);
    }
}
