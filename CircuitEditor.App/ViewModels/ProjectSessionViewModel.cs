using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Placement;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ProjectSessionViewModel : ObservableObject, IDisposable
{
    private readonly ProjectViewModel project;
    private readonly BreadboardViewModel mockup;
    private readonly SchematicViewModel schematic;
    private readonly INavigationService navigation;
    private readonly IProjectRepository repository;
    private readonly IRecentProjectsRepository recent;
    private readonly IDialogService dialogs;
    private readonly IAutosaveService autosave;
    private readonly IBreadboardRepository boards;
    private string? path;
    private readonly Core.Services.IComponentInsertionService insertion;
    public ProjectSessionViewModel(ProjectViewModel project, BreadboardViewModel mockup, SchematicViewModel schematic,
        INavigationService navigation, IProjectRepository repository, IRecentProjectsRepository recent, IDialogService dialogs,
        IAutosaveService autosave, IBreadboardRepository boards, Core.Services.IComponentInsertionService insertion)
    {
        this.insertion=insertion;
        this.project=project; this.mockup=mockup; this.schematic=schematic; this.navigation=navigation;
        this.repository=repository; this.recent=recent; this.dialogs=dialogs; this.autosave=autosave; this.boards=boards;
        foreach (var file in recent.Load()) RecentFiles.Add(new RecentProjectViewModel(file,OpenRecentCommand));
        project.PropertyChanged += OnProjectChanged;
    }
    public ObservableCollection<RecentProjectViewModel> RecentFiles { get; } = [];
    public string Title => $"{(project.IsDirty ? "* " : "")}{(path is null ? "Новый проект" : Path.GetFileName(path))} — Circuit Editor";
    public ProjectDocument Capture() => ProjectDocument.Capture(project.Model, new ProjectViewState(
        navigation.Current.Kind == WorkspaceKind.Schematic ? RepresentationKind.Schematic : RepresentationKind.Mockup,
        mockup.Zoom,mockup.OffsetX,mockup.OffsetY,schematic.Zoom,schematic.OffsetX,schematic.OffsetY));
    public void Initialize()
    {
        if (repository.HasRecovery && dialogs.ConfirmRecovery())
        {
            if (Load(repository.RecoveryPath))
            {
                path = null;
                // Recovery is an unsaved document even if history has just been reset.
                project.History.MarkUnsaved();
                project.StatusMessage = "Автосохранение восстановлено. Сохраните проект в .circ.";
            }
        }
        autosave.Start(Autosave);
    }
    public void Autosave()
    {
        if (!project.IsDirty || project.InGesture) return;
        if (repository.Save(repository.RecoveryPath, Capture()) is { } error) ReportError("Автосохранение: " + error);
    }
    public bool RequestClose()
    {
        if (!Guard()) return false;
        repository.ClearRecovery(); return true;
    }
    private bool Guard()
    {
        if (project.InGesture)
        {
            mockup.InputCommand.Execute(new Interaction.CanvasInput(Interaction.CanvasAction.Cancel));
            schematic.InputCommand.Execute(new Interaction.CanvasInput(Interaction.CanvasAction.Cancel));
        }
        if (!project.IsDirty) return true;
        return dialogs.ConfirmUnsaved() switch { SaveDecision.Save => SaveCurrent(false), SaveDecision.Discard => true, _ => false };
    }
    [RelayCommand]
    private void New()
    {
        if (!Guard()) return;
        project.Wiring.Cancel();
        var board = boards.Load();
        if (board.Value is null) { ReportError(board.Error ?? "Не удалось загрузить breadboard."); return; }
        project.ReplaceProject(new Core.Models.CircuitProject(footprints: project.Model.Footprints,
            breadboards: [new Core.Breadboards.BreadboardInstance(Guid.NewGuid(),board.Value,Core.Physical.PhysicalSettings.DefaultBoardX,Core.Physical.PhysicalSettings.DefaultBoardY)]));
        mockup.ResetViewCommand.Execute(null); schematic.ResetViewCommand.Execute(null);
        navigation.Navigate(WorkspaceKind.Mockup); path = null; repository.ClearRecovery(); OnPropertyChanged(nameof(Title));
    }
    [RelayCommand]
    private void Open() { if (!Guard()) return; var file = dialogs.OpenProject(); if (file is not null) OpenPath(file); }
    [RelayCommand]
    private void OpenRecent(string? file) { if (file is not null && Guard()) OpenPath(file); }
    private void OpenPath(string file)
    {
        if (!Load(file)) return;
        path = file; Remember(file); repository.ClearRecovery(); OnPropertyChanged(nameof(Title));
    }
    public bool Load(string file)
    {
        var data = repository.Load(file);
        if (data.Value is null) { ReportError(data.Error ?? "Не удалось открыть проект."); return false; }
        try
        {
            var board = boards.Load().Value ?? throw new ArgumentException("Библиотека breadboard недоступна.");
            var state = ProjectDocumentResolver.Resolve(data.Value, project.Library, project.Model.Footprints, board, insertion);
            project.Wiring.Cancel(); mockup.InputCommand.Execute(new Interaction.CanvasInput(Interaction.CanvasAction.Cancel));
            schematic.InputCommand.Execute(new Interaction.CanvasInput(Interaction.CanvasAction.Cancel));
            project.ReplaceProject(state); ApplyView(data.Value.View);
            project.StatusMessage = "Проект открыт.";
            if (data.Warning is { } warning) dialogs.ShowWarning(warning);
            return true;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or NullReferenceException)
        { ReportError("Не удалось открыть проект: " + e.Message); return false; }
    }
    private void ApplyView(ProjectViewState view)
    {
        mockup.Zoom = view.MockupZoom; mockup.OffsetX = view.MockupX; mockup.OffsetY = view.MockupY;
        schematic.Zoom = view.SchematicZoom; schematic.OffsetX = view.SchematicX; schematic.OffsetY = view.SchematicY;
        navigation.Navigate(view.Active == RepresentationKind.Mockup ? WorkspaceKind.Mockup : WorkspaceKind.Schematic);
        project.Refresh();
    }
    [RelayCommand] private void Save() => SaveCurrent(false);
    [RelayCommand] private void SaveAs() => SaveCurrent(true);
    private bool SaveCurrent(bool forceDialog)
    {
        if (project.InGesture) { ReportError("Завершите текущий жест перед сохранением."); return false; }
        var target = forceDialog || path is null ? dialogs.SaveFile("Circuit Editor (*.circ)|*.circ", ".circ", path is null ? null : Path.GetFileName(path)) : path;
        if (target is null) return false;
        var error = repository.Save(target, Capture());
        if (error is not null) { ReportError(error); return false; }
        path = target; project.History.MarkSaved(); Remember(target); repository.ClearRecovery();
        project.StatusMessage = "Проект сохранён."; OnPropertyChanged(nameof(Title)); return true;
    }
    private void ReportError(string message)
    { project.StatusMessage = message; dialogs.ShowError(message); }
    private void Remember(string file)
    { recent.Add(file); RecentFiles.Clear(); foreach (var item in recent.Load()) RecentFiles.Add(new RecentProjectViewModel(item,OpenRecentCommand)); }
    private void OnProjectChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(project.IsDirty)) OnPropertyChanged(nameof(Title)); }
    public void Dispose() { autosave.Dispose(); project.PropertyChanged -= OnProjectChanged; }
}
