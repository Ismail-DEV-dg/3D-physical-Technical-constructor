using CircuitEditor.App.Services;
using CircuitEditor.Core.Export;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ExportViewModel(ProjectViewModel project, INavigationService navigation, IDialogService dialogs,
    IImageExportService images, ISvgExportService svg, IBomExportService bom, IThreeDExportService? threeD = null)
{
    private void Report(string? error, string success)
    { project.StatusMessage = error ?? success; if (error is not null) dialogs.ShowError(error); }
    [RelayCommand]
    private void Png()
    {
        var kind = navigation.Current.Kind;
        if (kind is not (WorkspaceKind.Mockup or WorkspaceKind.Schematic or WorkspaceKind.ThreeD))
        { Report("Для PNG выберите Макет, Схему или 3D.", ""); return; }
        var scale = dialogs.ChooseExportScale(kind == WorkspaceKind.ThreeD); if (scale is null) return;
        var file = dialogs.SaveFile("PNG (*.png)|*.png",".png"); if (file is null) return;
        var error = kind == WorkspaceKind.ThreeD
            ? threeD is null ? "Экспорт 3D недоступен." : threeD.ExportPng(file, scale.Value)
            : images.ExportPng(file, kind, scale.Value);
        Report(error, "PNG экспортирован.");
    }
    [RelayCommand]
    private void Obj()
    {
        if (navigation.Current.Kind != WorkspaceKind.ThreeD || threeD is null)
        { Report("Для OBJ выберите вкладку 3D.", ""); return; }
        var file = dialogs.SaveFile("Wavefront OBJ (*.obj)|*.obj", ".obj");
        if (file is not null) Report(threeD.ExportObj(file), "OBJ и материалы экспортированы.");
    }
    [RelayCommand]
    private void Svg()
    {
        if (navigation.Current.Kind != WorkspaceKind.Schematic) { Report("SVG доступен только для Схемы.", ""); return; }
        var file = dialogs.SaveFile("SVG (*.svg)|*.svg",".svg"); if (file is null) return;
        Report(svg.ExportSchematic(file), "SVG схемы экспортирован.");
    }
    [RelayCommand]
    private void Bom()
    {
        var file = dialogs.SaveFile("CSV (*.csv)|*.csv",".csv"); if (file is null) return;
        Report(bom.Export(file,project.Model), "BOM экспортирован в CSV.");
    }
}
