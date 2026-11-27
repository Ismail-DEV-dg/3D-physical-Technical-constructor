using CircuitEditor.App.Services;
using CircuitEditor.Core.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class GuideBrowserViewModel(GuideLibrary library, IClipboardService clipboard) : ObservableObject
{
    private IReadOnlyList<GuideChapter> all = [];
    [ObservableProperty] private string search = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasCode)), NotifyPropertyChangedFor(nameof(HasChapter))]
    private GuideChapter? chapter;
    [ObservableProperty] private string feedback = "";

    public IReadOnlyList<GuideChapter> Chapters => all.Where(c => Search.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .All(term => (c.Id + " " + c.Title + " " + c.Instructions + " " + c.Code).Contains(term, StringComparison.OrdinalIgnoreCase))).ToArray();

    public bool HasCode => Chapter?.Code is not null;
    public bool HasChapter => Chapter is not null;
    public string CodeHeading => Chapter?.CodePurpose ?? "Скетч для Arduino IDE";
    public string ChapterPosition => Chapter is null ? "" : $"{CurrentIndex + 1} из {Chapters.Count}";
    private int CurrentIndex => Chapters.ToList().IndexOf(Chapter!);
    private bool HasPrevious => CurrentIndex > 0;
    private bool HasNext => CurrentIndex >= 0 && CurrentIndex < Chapters.Count - 1;

    internal void Open(string section, string? id) => Open(library.Chapters(section), id);

    internal void Open(IReadOnlyList<GuideChapter> chapters, string? id = null)
    {
        all = chapters;
        Search = "";
        OnPropertyChanged(nameof(Chapters));
        Chapter = all.FirstOrDefault(c => c.Id == id) ?? all.FirstOrDefault();
        Feedback = "";
    }

    partial void OnSearchChanged(string value)
    {
        OnPropertyChanged(nameof(Chapters));
        if (Chapter is null || !Chapters.Contains(Chapter)) Chapter = Chapters.FirstOrDefault();
        NotifyNavigation();
    }

    partial void OnChapterChanged(GuideChapter? value)
    {
        Feedback = "";
        OnPropertyChanged(nameof(CodeHeading));
        NotifyNavigation();
    }

    private void NotifyNavigation()
    {
        OnPropertyChanged(nameof(ChapterPosition));
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasPrevious))]
    private void Previous() => Chapter = Chapters[CurrentIndex - 1];

    [RelayCommand(CanExecute = nameof(HasNext))]
    private void Next() => Chapter = Chapters[CurrentIndex + 1];

    [RelayCommand]
    private void CopyCode()
    {
        if (Chapter?.Code is { } code)
            Feedback = clipboard.TryCopy(code, out var error) ? "Скетч скопирован." : error ?? "Не удалось скопировать.";
    }
}
