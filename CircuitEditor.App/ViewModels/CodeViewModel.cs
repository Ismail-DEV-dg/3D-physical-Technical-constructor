using System.ComponentModel;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class CodeViewModel : WorkspaceViewModel, IDisposable
{
    private readonly ProjectViewModel? project;
    private readonly IThemeService? themes;
    private readonly ISketchRepository? sketches;
    private readonly IDialogService? dialogs;
    private readonly IReadOnlyList<CodeCompletionItem> api;
    private readonly CodeCatalog? catalog;
    private readonly ICodeAnalysisService? analysisService;
    private CancellationTokenSource? analysisCancellation;
    private bool disposed;
    private const int AnalysisDelayMilliseconds=350;
    private SketchDocument detached=new();
    private SketchDocument observed=new();
    public CodeViewModel(ProjectViewModel? project=null,ICodeCompletionRepository? completion=null,
        ICodeTemplateRepository? templates=null,IThemeService? themes=null,ISketchRepository? sketches=null,IDialogService? dialogs=null,
        ICodeAnalysisService? analysisService=null,IClipboardService? clipboard=null)
        :base(WorkspaceKind.Code,"Код","05","Arduino / ESP32","Редактор скетчей Arduino с учебным запуском GPIO.")
    {
        this.project=project;this.themes=themes;this.sketches=sketches;this.dialogs=dialogs;
        Output=new(clipboard);
        this.analysisService=analysisService;ProblemsPanel.NavigateRequested+=NavigateToProblem;
        observed=Sketch;
        var loaded=completion?.Load();api=loaded?.Value??[];catalog=templates?.Load().Value;
        if(loaded?.Error is { } error)Output.Append(error);
        if(project is not null){project.SketchChanged+=SketchChanged;project.ModelCommitted+=CircuitChanged;project.ProjectReplaced+=Replaced;}
        if(themes is not null)themes.PropertyChanged+=ThemeChanged;
    }
    public SketchDocument Sketch=>project?.Model.Sketch??detached;
    public OutputViewModel Output {get;}
    public CodeProblemsViewModel ProblemsPanel {get;}=new();
    public Task AnalysisTask {get;private set;}=Task.CompletedTask;
    public void RequestAnalysis()=>ScheduleAnalysis();
    [ObservableProperty] private bool isAnalyzing;
    [ObservableProperty] private bool canUndo;
    [ObservableProperty] private bool canRedo;
    [ObservableProperty] private double editorFontSize=13;
    public IReadOnlyList<BoardKind> Boards {get;}=Enum.GetValues<BoardKind>();
    public IReadOnlyList<Core.Models.Component> Controllers=>project?.Model.Circuit.Components.Where(c=>catalog?.Boards.Any(b=>b.ComponentId==c.Definition.Id&&b.Kind==Board)==true).ToArray()??[];
    public string ControllerHint=>Controllers.Count==1?"Авто":"Плата…";
    public string Text {get=>Sketch.Text;set{if(value.Length>SketchDocument.MaximumTextLength){Output.Append("Текст слишком большой; данные проекта не изменены.");return;}if(value!=Text)Update(Sketch with {Text=value});}}
    public BoardKind Board {get=>Sketch.Board;set{if(value!=Board)Update(Sketch with {Board=value,ControllerComponentId=null});}}
    public Guid? ControllerId {get=>Sketch.ControllerComponentId;set{if(value!=ControllerId)Update(Sketch with {ControllerComponentId=value});}}
    public string SketchName {get=>Sketch.Name;set{if(!string.IsNullOrWhiteSpace(value)&&value!=SketchName)Update(Sketch with {Name=value});}}
    public IReadOnlyList<CodeCompletionItem> Completions=>api.Concat((project?.Model.Circuit.Components??[]).SelectMany(c=>new[] {
        new CodeCompletionItem(c.Reference,"Компонент схемы: "+c.Definition.Name,"Project",[]),
        new CodeCompletionItem(c.Reference.ToUpperInvariant()+"_PIN","Имя пина компонента; требуется соответствующее объявление.","Project",[])}))
        .Concat(catalog?.Boards.Single(b=>b.Kind==Board).Pins.Where(p=>p.IsExposed).Select(p=>new CodeCompletionItem(p.Code,p.Id,"Pin",[]))??[])
        .Concat(CppTokenScanner.Scan(Text).GroupBy(t=>t.Line).Select(g=>g.ToArray()).Where(p=>p.Length>=3&&p[0].Text=="#"&&p[1].Text=="define")
            .Select(p=>new CodeCompletionItem(p[2].Text,"Константа скетча","Project",[]))).DistinctBy(i=>i.Text).ToArray();
    [ObservableProperty] private int documentVersion;
    [ObservableProperty] private int themeVersion;
    [ObservableProperty] private int navigationVersion;
    [ObservableProperty] private bool isFindOpen;
    [ObservableProperty] private bool isReplaceOpen;
    [ObservableProperty] private bool isGoToOpen;
    [ObservableProperty] private string searchText="";
    [ObservableProperty] private string replacementText="";
    [ObservableProperty] private string goToText="1";
    [ObservableProperty] private string searchMessage="";
    [ObservableProperty] private bool matchCase;
    [ObservableProperty] private int cursorLine=1;
    [ObservableProperty] private int cursorColumn=1;
    public string CursorStatus=>$"Строка {CursorLine}, столбец {CursorColumn}";
    partial void OnCursorLineChanged(int value)=>OnPropertyChanged(nameof(CursorStatus));
    partial void OnCursorColumnChanged(int value)=>OnPropertyChanged(nameof(CursorStatus));
    public int RequestedLine {get;private set;}=1;
    public int RequestedColumn {get;private set;}=1;
    public event EventHandler<string>? EditorActionRequested;
    public event EventHandler? ShowCodeRequested;
    public void RevealProblem(CodeProblem problem)=>NavigateToProblem(this,problem);
    [ObservableProperty] private string operationMessage="";
    private void NavigateToProblem(object? sender,CodeProblem value)
    {
        RequestedLine=value.Line;RequestedColumn=value.Column;
        ShowCodeRequested?.Invoke(this,EventArgs.Empty);NavigationVersion++;
    }
    [RelayCommand] private void Find(){IsFindOpen=true;IsGoToOpen=false;EditorActionRequested?.Invoke(this,"FocusFind");}
    [RelayCommand] private void Replace(){IsReplaceOpen=true;Find();}
    [RelayCommand] private void GoTo(){IsFindOpen=false;IsReplaceOpen=false;IsGoToOpen=true;SearchMessage="";EditorActionRequested?.Invoke(this,"FocusLine");}
    [RelayCommand] private void CloseSearch(){IsFindOpen=false;IsReplaceOpen=false;IsGoToOpen=false;EditorActionRequested?.Invoke(this,"FocusEditor");}
    [RelayCommand] private void EditorAction(string action)=>EditorActionRequested?.Invoke(this,action);
    [RelayCommand] private void Export()
    {
        var path=dialogs?.SaveFile("Arduino sketch (*.ino)|*.ino",".ino",SketchName+".ino");if(path is null||sketches is null)return;
        EnsureEntryPoints();
        var result=sketches.Export(path,Text);if(result.Error is { } error)dialogs?.ShowError(error);else OperationMessage="Экспортировано: "+result.Value;
    }
    internal void EnsureEntryPoints() => Update(Sketch.WithEntryPoints());
    private void Update(SketchDocument sketch)
    {if(project is null){detached=sketch;NotifySketch();}else project.UpdateSketch(sketch);}
    private void SketchChanged(object? sender,EventArgs e){NotifySketch();ScheduleAnalysis();}
    private void CircuitChanged(object? sender,EventArgs e){OnPropertyChanged(nameof(Controllers));OnPropertyChanged(nameof(ControllerHint));OnPropertyChanged(nameof(Completions));ProblemsPanel.Clear();ScheduleAnalysis();}
    private void Replaced(object? sender,EventArgs e){DocumentVersion++;NavigationVersion=0;ProblemsPanel.Clear();NotifySketch();ScheduleAnalysis();}
    private void ThemeChanged(object? sender,PropertyChangedEventArgs e)=>ThemeVersion++;
    private void NotifySketch()
    {
        var previous=observed;observed=Sketch;
        if(previous.Text!=observed.Text)OnPropertyChanged(nameof(Text));
        if(previous.Name!=observed.Name)OnPropertyChanged(nameof(SketchName));
        if(previous.Board!=observed.Board)
        {OnPropertyChanged(nameof(Board));OnPropertyChanged(nameof(Controllers));OnPropertyChanged(nameof(ControllerHint));}
        if(previous.ControllerComponentId!=observed.ControllerComponentId)OnPropertyChanged(nameof(ControllerId));
        OnPropertyChanged(nameof(Completions));
    }
    private void ScheduleAnalysis()
    {
        analysisCancellation?.Cancel();analysisCancellation?.Dispose();
        if(analysisService is null||project is null||disposed)return;
        analysisCancellation=new();AnalysisTask=AnalyzeAfterDelay(analysisCancellation.Token);
    }
    private async Task AnalyzeAfterDelay(CancellationToken token)
    {
        IsAnalyzing=true;
        try
        {
            await Task.Delay(AnalysisDelayMilliseconds,token);
            if(project!.InGesture)return;
            var sketch=Sketch;var snapshot=new Core.History.ProjectSnapshot(project.Model).Restore();snapshot.SetSketch(sketch);
            var result=await analysisService!.AnalyzeAsync(snapshot,sketch,token);
            if(token.IsCancellationRequested||disposed)return;
            ProblemsPanel.SetAnalysis(result);
        }
        catch(OperationCanceledException) { }
        catch(Exception e) {if(!disposed&&!token.IsCancellationRequested)Output.Append("Анализ кода не выполнен: "+e.Message);}
        finally {if(!disposed&&!token.IsCancellationRequested)IsAnalyzing=false;}
    }
    public void Dispose()
    {
        disposed=true;analysisCancellation?.Cancel();analysisCancellation?.Dispose();ProblemsPanel.NavigateRequested-=NavigateToProblem;
        if(project is not null){project.SketchChanged-=SketchChanged;project.ModelCommitted-=CircuitChanged;project.ProjectReplaced-=Replaced;}
        if(themes is not null)themes.PropertyChanged-=ThemeChanged;
    }
}
