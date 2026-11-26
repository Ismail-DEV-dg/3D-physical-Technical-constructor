using System.IO;
using System.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

internal sealed partial class FilePickerViewModel : ObservableObject
{
    private readonly bool save;
    private readonly string extension;
    private readonly Action<string?> complete;
    private readonly Func<string, bool> confirmOverwrite;
    private string directory = Directory.GetCurrentDirectory();

    [ObservableProperty] private string address = "";
    [ObservableProperty] private string fileName;
    [ObservableProperty] private string error = "";
    [ObservableProperty] private IReadOnlyList<FilePickerEntry> entries = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand))]
    private FilePickerEntry? selected;
    [ObservableProperty] private FilePickerFilter selectedFilter;
    [ObservableProperty] private FilePickerEntry? selectedLocation;
    [ObservableProperty] private bool canGoUp;
    [ObservableProperty] private string newFolderName = "";
    [ObservableProperty] private bool isCreatingFolder;

    public FilePickerViewModel(string filter, string? saveExtension, string? suggested, string? initialDirectory,
        Action<string?> complete, Func<string, bool> confirmOverwrite)
    {
        save = saveExtension is not null;
        extension = string.IsNullOrWhiteSpace(saveExtension) ? "" : "." + saveExtension.TrimStart('.');
        this.complete = complete;
        this.confirmOverwrite = confirmOverwrite;
        fileName = suggested ?? "";
        Filters = FilePickerFilter.Parse(filter);
        selectedFilter = Filters[0];
        Locations = GetLocations();
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!Navigate(initialDirectory ?? documents))
            Navigate(directory);
    }

    public string Title => save ? "Сохранить файл" : "Открыть файл";
    public bool IsSaving => save;
    public string ActionLabel => save ? "Сохранить" : "Открыть";
    public IReadOnlyList<FilePickerFilter> Filters { get; }
    public IReadOnlyList<FilePickerEntry> Locations { get; }

    private static IReadOnlyList<FilePickerEntry> GetLocations()
    {
        var locations = new List<FilePickerEntry>();
        void Add(Environment.SpecialFolder folder, string label)
        {
            var path = Environment.GetFolderPath(folder);
            if (Directory.Exists(path))
                locations.Add(new(label, path, true));
        }

        Add(Environment.SpecialFolder.DesktopDirectory, "Рабочий стол");
        Add(Environment.SpecialFolder.MyDocuments, "Документы");
        Add(Environment.SpecialFolder.UserProfile, "Папка пользователя");
        locations.AddRange(Directory.GetLogicalDrives().Select(drive => new FilePickerEntry(drive, drive, true)));
        return locations;
    }

    partial void OnSelectedChanged(FilePickerEntry? value)
    {
        if (value is { IsDirectory: false })
            FileName = value.Name;
    }

    partial void OnSelectedLocationChanged(FilePickerEntry? value)
    {
        if (value is null) return;
        Navigate(value.FullPath);
        SelectedLocation = null;
    }

    partial void OnSelectedFilterChanged(FilePickerFilter value) => Navigate(directory);

    [RelayCommand]
    private void GoToAddress() => Navigate(Address);

    [RelayCommand]
    private void Up()
    {
        var parent = Directory.GetParent(directory);
        if (parent is not null)
            Navigate(parent.FullName);
    }

    private bool Navigate(string address)
    {
        try
        {
            var target = Resolve(address);
            var browser = new DirectoryInfo(target);
            var listing = browser.EnumerateFileSystemInfos()
                .Where(item => item is DirectoryInfo || SelectedFilter.Includes(item.Name))
                .Select(item => new FilePickerEntry(item.Name, item.FullName, item is DirectoryInfo))
                .OrderByDescending(item => item.IsDirectory)
                .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();

            if (!save && !StringComparer.OrdinalIgnoreCase.Equals(directory, target))
                FileName = "";
            directory = target;
            Address = target;
            CanGoUp = browser.Parent is not null;
            Selected = null;
            Entries = listing;
            Error = "";
            return true;
        }
        catch (Exception ex) when (IsPathError(ex))
        {
            Error = "Не удалось открыть папку. Проверьте путь и права доступа.";
            return false;
        }
    }

    private bool CanOpenSelected() => Selected is not null;

    [RelayCommand(CanExecute = nameof(CanOpenSelected))]
    private void OpenSelected() => OpenEntry(Selected);

    [RelayCommand]
    private void OpenEntry(FilePickerEntry? entry)
    {
        if (entry is { IsDirectory: true } folder)
            Navigate(folder.FullPath);
        else if (entry is { IsDirectory: false })
            AcceptPath(entry.FullPath);
    }

    [RelayCommand]
    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            Error = "Укажите имя файла.";
            return;
        }

        AcceptPath(FileName);
    }

    private void AcceptPath(string name)
    {
        try
        {
            var path = Resolve(name);
            if (Directory.Exists(path))
            {
                Navigate(path);
                return;
            }

            if (!save && !File.Exists(path))
            {
                Error = "Файл не найден.";
                return;
            }

            if (save)
            {
                if (!Path.HasExtension(path))
                    path += extension;
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Error = "Папка для сохранения не найдена.";
                    return;
                }
                if (Path.GetFileName(path).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    Error = "Имя файла содержит недопустимые символы.";
                    return;
                }
                if (File.Exists(path) && !confirmOverwrite(path))
                    return;
            }

            complete(path);
        }
        catch (Exception ex) when (IsPathError(ex))
        {
            Error = "Проверьте имя файла и путь.";
        }
    }

    [RelayCommand]
    private void ShowNewFolder()
    {
        NewFolderName = "";
        IsCreatingFolder = true;
    }

    [RelayCommand]
    private void CreateFolder()
    {
        var name = NewFolderName.Trim();
        if (name is "" or "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name.EndsWith('.'))
        {
            Error = "Укажите допустимое имя новой папки.";
            return;
        }

        try
        {
            var path = Path.Combine(directory, name);
            if (Directory.Exists(path) || File.Exists(path))
            {
                Error = "Такое имя уже используется в этой папке.";
                return;
            }

            Directory.CreateDirectory(path);
            if (Navigate(path)) IsCreatingFolder = false;
        }
        catch (Exception ex) when (IsPathError(ex))
        {
            Error = "Не удалось создать папку. Проверьте имя и права доступа.";
        }
    }

    [RelayCommand]
    private void CancelNewFolder()
    {
        IsCreatingFolder = false;
        Error = "";
    }

    [RelayCommand]
    private void Cancel() => complete(null);

    private string Resolve(string path) =>
        Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')), directory);

    private static bool IsPathError(Exception exception) =>
        exception is ArgumentException or IOException or NotSupportedException or SecurityException or UnauthorizedAccessException;
}
