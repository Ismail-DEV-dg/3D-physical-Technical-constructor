using CircuitEditor.App.Services;
using CircuitEditor.Core.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class GuideBrowserViewModel(GuideLibrary library, IClipboardService clipboard) : ObservableObject
{
    private IReadOnlyList<GuideChapter> all = [];
    [ObservableProperty] private string search = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasCode))] private GuideChapter? chapter;
    [ObservableProperty] private string feedback = "";

    public IReadOnlyList<GuideChapter> Chapters => all.Where(c => Search.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .All(term => (c.Id + " " + c.Title + " " + c.Instructions + " " + c.Code).Contains(term, StringComparison.OrdinalIgnoreCase))).ToArray();

    public bool HasCode => Chapter?.Code is not null;

    internal void Open(string section, string? id)
    {
        all = library.Chapters(section);
        Search = "";
        OnPropertyChanged(nameof(Chapters));
        Chapter = all.FirstOrDefault(c => c.Id == id) ?? all.FirstOrDefault();
        Feedback = "";
    }

    partial void OnSearchChanged(string value)
    {
        OnPropertyChanged(nameof(Chapters));
        if (Chapter is null || !Chapters.Contains(Chapter)) Chapter = Chapters.FirstOrDefault();
    }

    [RelayCommand]
    private void CopyCode()
    {
        if (Chapter?.Code is { } code)
            Feedback = clipboard.TryCopy(code, out var error) ? "Скетч скопирован." : error ?? "Не удалось скопировать.";
    }
}
