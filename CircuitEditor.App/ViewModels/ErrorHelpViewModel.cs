using CircuitEditor.App.Services;
using CircuitEditor.Core.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ErrorHelpViewModel : ObservableObject
{
    private readonly IErrorDocumentationService documentation;
    private readonly IClipboardService clipboard;
    private DiagnosticArticle? previousError;
    private string section = "home";

    public ErrorHelpViewModel(IErrorDocumentationService documentation, IClipboardService clipboard,
        GuideLibrary? library = null, string? libraryError = null)
    {
        this.documentation = documentation;
        this.clipboard = clipboard;
        Guide = new(library ?? new(), clipboard);
        LibraryError = libraryError ?? "";
    }

    public GuideBrowserViewModel Guide { get; }
    public string LibraryError { get; }
    [ObservableProperty] private string search = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArticleText)), NotifyPropertyChangedFor(nameof(HasRelatedComponent)),
        NotifyPropertyChangedFor(nameof(HasRelatedApplication))]
    private DiagnosticArticle? selected;
    [ObservableProperty] private string feedback = "";

    public bool IsHome => section == "home";
    public bool IsErrors => section == "errors";
    public bool IsGuide => !IsHome && !IsErrors;
    public bool HasRelatedComponent => Selected?.ComponentGuideId is not null;
    public bool HasRelatedApplication => Selected?.ApplicationGuideId is not null;
    public string Heading => section switch
    {
        "errors" => "Ошибки и способы исправления",
        "nano" => "Пособие по Arduino Nano",
        "uno" => "Пособие по Arduino Uno",
        "esp32" => "Пособие по ESP32 DevKit",
        "application" => "Руководство по Circuit Editor",
        _ => "Справка Circuit Editor"
    };

    public IReadOnlyList<DiagnosticArticle> Articles => documentation.Articles.Where(a =>
        Search.Contains("[" + a.Code + "]", StringComparison.OrdinalIgnoreCase) || Search.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(term =>
            (a.Code + " " + a.Title + " " + a.Cause + " " + a.Fix + " " + string.Join(" ", a.Keywords))
                .Contains(term, StringComparison.OrdinalIgnoreCase))).ToArray();

    public string CountSummary => $"Найдено статей: {Articles.Count}";
    public string ArticleText => Selected is { } article
        ? $"{article.Title}\nКод: {article.Code}\n\nПочему возникает\n{article.Cause}\n\nКак исправить\n{article.Fix}"
        : "По запросу ничего не найдено. Попробуйте код ошибки или название компонента.";

    partial void OnSearchChanged(string value)
    {
        OnPropertyChanged(nameof(Articles));
        OnPropertyChanged(nameof(CountSummary));
        if (Selected is null || !Articles.Contains(Selected)) Selected = Articles.FirstOrDefault();
    }

    public void SelectCode(string? code)
    {
        OpenSection("errors");
        if (code is not null) Selected = documentation.Resolve(code);
    }

    [RelayCommand]
    public void OpenSection(string name)
    {
        previousError = null;
        Navigate(name);
    }

    private void Navigate(string name, string? chapter = null)
    {
        section = name;
        Feedback = "";
        OnPropertyChanged(nameof(IsHome));
        OnPropertyChanged(nameof(IsErrors));
        OnPropertyChanged(nameof(IsGuide));
        OnPropertyChanged(nameof(Heading));
        if (IsErrors) { Search = ""; Selected = Articles.FirstOrDefault(); }
        if (IsGuide) Guide.Open(name, chapter);
    }

    [RelayCommand]
    private void OpenRelated(string name)
    {
        if (Selected is not { } article) return;
        var chapter = name == "application" ? article.ApplicationGuideId : article.ComponentGuideId;
        if (chapter is null) return;
        previousError = article;
        Navigate(name, chapter);
    }

    [RelayCommand]
    private void Back()
    {
        if (previousError is { } article)
        {
            SelectCode(article.Code);
            return;
        }

        if (IsHome) Close();
        else OpenSection("home");
    }

    public event EventHandler? CloseRequested;
    public event EventHandler? FocusSearchRequested;
    [RelayCommand] private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void FocusSearch() => FocusSearchRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Copy() => Feedback = clipboard.TryCopy(ArticleText, out var error)
        ? "Статья скопирована." : error ?? "Не удалось скопировать.";
}
