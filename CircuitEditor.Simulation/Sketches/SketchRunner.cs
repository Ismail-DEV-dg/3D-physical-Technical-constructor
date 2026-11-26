using CircuitEditor.Core.Code;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Sketches;

public sealed class SketchRunner(ICodeTemplateRepository templates) : ISketchRunner
{
    public CodeProblem? Start(SketchDocument sketch, SimulationSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sketch.Text))
            return Error("Введите скетч с функциями setup() и loop().");
        var loaded = templates.Load();
        if (loaded.Value is not { } catalog) return Error(loaded.Error ?? "Таблица пинов недоступна.");

        var profile = catalog.Boards.Single(b => b.Kind == sketch.Board);
        var boards = session.Circuit.Project.Circuit.Components.Where(c => c.Definition.Id == profile.ComponentId &&
            (sketch.ControllerComponentId is null || c.Id == sketch.ControllerComponentId)).ToArray();
        if (boards.Length != 1 || !session.Circuit.Controllers.ContainsKey(boards[0].Id))
            return Error(boards.Length > 1
                ? $"Во вкладке «Код» выберите экземпляр платы {sketch.Board}."
                : $"Добавьте на макет плату {sketch.Board} или выберите существующий экземпляр во вкладке «Код».");

        try
        {
            var controller = boards[0].Id;
            var program = new SketchParser(sketch.Text, profile, cancellationToken, session, controller).Parse(controller);
            new SketchExecution(program, session).Start();
            return null;
        }
        catch (SketchSyntaxException error) { return error.Problem; }
    }

    private static CodeProblem Error(string message) => new(message, Severity: "Error", Code: "CODE_RUN");
}
