using System.IO;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneExportAdapter(IExportRepository repository) : IThreeDExportService, ISceneViewportRegistration
{
    private SceneViewport? current;

    public void Attach(SceneViewport viewport) => current = viewport;

    public void Detach(SceneViewport viewport)
    {
        if (ReferenceEquals(current, viewport)) current = null;
    }

    public string? ExportPng(string path, double scale)
    {
        if (current is not { HasScene: true, Viewport: { } view } viewport) return Unavailable;
        try { return repository.Write(path, ScenePng.Capture(view, viewport.Background, scale)); }
        catch (Exception error) when (IsExportFailure(error)) { return "Ошибка экспорта PNG: " + error.Message; }
    }

    public string? ExportObj(string path)
    {
        if (current is not { HasScene: true, Viewport: { } view }) return Unavailable;
        try { return SceneObj.Write(view, path, repository); }
        catch (Exception error) when (IsExportFailure(error)) { return "Ошибка экспорта OBJ: " + error.Message; }
    }

    private const string Unavailable = "Откройте вкладку 3D и дождитесь построения сцены.";
    private static bool IsExportFailure(Exception error) =>
        error is ArgumentException or InvalidOperationException or NotSupportedException
            or IOException or UnauthorizedAccessException or OverflowException;
}
