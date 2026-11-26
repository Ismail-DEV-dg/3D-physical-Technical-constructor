using CircuitEditor.App.Services;
using CircuitEditor.Core.Export;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ExportViewModel(ProjectViewModel project, INavigationService navigation, IDialogService dialogs,
    IImageExportService images, ISvgExportService svg, IBomExportService bom)
{
    private void Report(string? error, string success)
    { project.StatusMessage = error ?? success; if (error is not null) dialogs.ShowError(error); }
    [RelayCommand]
    private void Png()
    {
        var kind = navigation.Current.Kind;
        if (kind is not (WorkspaceKind.Mockup or WorkspaceKind.Schematic))
        { Report("Для PNG выберите Макет или Схему.", ""); return; }
        var scale = dialogs.ChooseExportScale(); if (scale is null) return;
        var file = dialogs.SaveFile("PNG (*.png)|*.png",".png"); if (file is null) return;
        Report(images.ExportPng(file, kind, scale.Value), "PNG экспортирован.");
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
