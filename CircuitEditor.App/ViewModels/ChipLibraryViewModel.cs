using CircuitEditor.App.Services;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ChipLibraryViewModel : ObservableObject
{
    private const string Filter = "Микросхема (*.chip)|*.chip";
    private readonly ChipViewModel editor;
    private readonly IUserChipLibraryService? catalog;
    private readonly IChipRepository? storage;
    private readonly IDialogService? dialogs;
    [ObservableProperty] private ChipDefinition? selectedTemplate;
    [ObservableProperty] private ChipDefinition? selectedChip;
    [ObservableProperty] private string copyName = "";

    public ChipLibraryViewModel(ChipViewModel editor, IUserChipLibraryService? catalog, IChipRepository? storage,
        IChipTemplateRepository? templates, IDialogService? dialogs)
    {
        this.editor = editor;
        this.catalog = catalog;
        this.storage = storage;
        this.dialogs = dialogs;
        if (catalog is not null) catalog.Changed += (_, _) => OnPropertyChanged(nameof(Saved));
        var templatesResult = templates?.Load();
        Templates = templatesResult?.Value ?? [];
        if (templatesResult?.Error is { } error) editor.Message = error;
    }

    public IReadOnlyList<ChipDefinition> Templates { get; }
    public IReadOnlyList<ChipDefinition> Saved => catalog?.Definitions ?? [];

    [RelayCommand]
    private void UseTemplate()
    {
        if (SelectedTemplate is { } template)
            editor.LoadDraft(template with { Id = Guid.NewGuid(), DefinitionVersion = 1 });
    }

    [RelayCommand]
    private void EditSaved()
    {
        if (SelectedChip is { } chip) editor.LoadDraft(chip);
    }

    [RelayCommand]
    private void Save()
    {
        if (storage is null || !editor.CheckReady()) return;
        var current = editor.Definition;
        var existing = Saved.FirstOrDefault(chip => chip.Id == current.Id);
        if (existing is not null && existing.DefinitionVersion != current.DefinitionVersion)
        {
            Error("В библиотеке уже есть другая ревизия. Откройте её или сохраните дубликат.");
            return;
        }
        if (existing is not null && ChipContent.Matches(current, existing))
        {
            editor.Message = "Эта ревизия уже есть в библиотеке.";
            return;
        }
        if (existing?.DefinitionVersion == int.MaxValue) { Error("Исчерпан диапазон номеров ревизий."); return; }
        var version = existing is null ? current : current with { DefinitionVersion = current.DefinitionVersion + 1 };
        if (storage.Store(version, existing?.DefinitionVersion) is { } problem) { Error(problem); return; }
        Refresh(version.Id);
        editor.UpdateRevision(version);
        editor.Message = $"{version.Name} сохранена в библиотеке, ревизия {version.DefinitionVersion}.";
    }

    [RelayCommand]
    private void Duplicate()
    {
        if (storage is null || !editor.CheckReady()) return;
        var name = CopyName.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.Equals(name, editor.Definition.Name, StringComparison.OrdinalIgnoreCase))
        {
            editor.Message = "Введите другое имя дубликата.";
            return;
        }
        var copy = editor.Definition with { Id = Guid.NewGuid(), DefinitionVersion = 1, Name = name };
        if (storage.Store(copy) is { } problem) { Error(problem); return; }
        Refresh(copy.Id);
        editor.LoadDraft(copy);
        editor.Message = $"{copy.Name}: дубликат сохранён.";
    }

    [RelayCommand]
    private void Import()
    {
        if (storage is null || dialogs?.OpenFile(Filter) is not { } path) return;
        var imported = storage.Load(path);
        if (imported.Value is not { } chip) { Error(imported.Error!); return; }
        var existing = Saved.FirstOrDefault(local => local.Id == chip.Id);
        if (existing is not null)
        {
            if (!ChipContent.Matches(chip, existing))
            {
                editor.LoadDraft(chip);
                Error("Такой ID уже используется. Импортированное определение открыто в редакторе. Сохраните его дубликатом.");
                return;
            }
        }
        else if (storage.Store(chip) is { } problem) { Error(problem); return; }
        Refresh(chip.Id);
        editor.LoadDraft(chip);
        editor.Message = $"{chip.Name}: микросхема доступна в палитре.";
    }

    [RelayCommand]
    private void Export()
    {
        if (storage is null || !editor.CheckReady() || dialogs?.SaveFile(Filter, ".chip", editor.Definition.Name) is not { } path) return;
        if (storage.Export(path, editor.Definition) is { } problem) Error(problem);
    }

    [RelayCommand]
    private void Delete()
    {
        if (storage is null || SelectedChip is not { } chip || dialogs?.ConfirmDeleteChip(chip.Name) != true) return;
        if (storage.Delete(chip.Id, chip.DefinitionVersion) is { } problem) { Error(problem); return; }
        Refresh(null);
        editor.Message = $"{chip.Name}: удалена из пользовательской библиотеки.";
    }

    [RelayCommand]
    private void ReloadLibrary() => Refresh(SelectedChip?.Id);

    private void Refresh(Guid? select)
    {
        var warning = catalog?.Refresh();
        OnPropertyChanged(nameof(Saved));
        SelectedChip = Saved.FirstOrDefault(chip => chip.Id == select);
        if (warning is not null) dialogs?.ShowWarning(warning);
    }

    private void Error(string problem) { editor.Message = problem; dialogs?.ShowError(problem); }
}
