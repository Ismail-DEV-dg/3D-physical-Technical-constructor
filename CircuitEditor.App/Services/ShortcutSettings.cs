using System.Windows.Input;
using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.Services;

public sealed class ShortcutSettings : ObservableObject, IShortcutSettings
{
    private readonly IShortcutSettingsRepository? repository;
    private readonly ShortcutBinding[] effective = ShortcutCatalog.Bindings.ToArray();
    private static readonly KeyConverter Converter = new();

    public ShortcutSettings(IShortcutSettingsRepository? repository = null)
    {
        this.repository = repository;
        Bindings = Array.AsReadOnly(effective);
        if (repository is null) return;
        var overrides = repository.Load();
        for (var index = 0; index < effective.Length; index++)
        {
            var binding = effective[index];
            if (binding.CanCustomize && overrides.TryGetValue(SettingId(index), out var stored) &&
                stored?.Split('|') is [var keyName, var modifiersName] &&
                Enum.TryParse<Key>(keyName, out var key) &&
                Enum.TryParse<ModifierKeys>(modifiersName, out var modifiers) &&
                ValidateGesture(binding, key, modifiers) is null)
                effective[index] = WithGesture(binding, key, modifiers);
        }
        // A saved binding may occupy a key that another saved binding has vacated.
        // Check conflicts after loading every override, irrespective of catalog order.
        for (var index = 0; index < effective.Length; index++)
        {
            var binding = effective[index];
            var original = ShortcutCatalog.Bindings[index];
            if (binding == original || FindConflict(index, binding.Key, binding.Modifiers) is null) continue;
            effective[index] = original;
            index = -1;
        }
    }

    public IReadOnlyList<ShortcutBinding> Bindings { get; }

    public string this[string command]
    {
        get
        {
            var matches = Bindings.Where(binding => binding.Id == command).ToArray();
            return matches.Length == 0 ? "" : matches[0].Description + " · " +
                string.Join(" / ", matches.Select(binding => binding.Keys));
        }
    }

    public string? Assign(ShortcutBinding binding, Key key, ModifierKeys modifiers)
    {
        var index = Array.IndexOf(effective, binding);
        if (index < 0 || !binding.CanCustomize) return "Для этого действия сочетание фиксировано.";
        if ((ValidateGesture(binding, key, modifiers) ?? FindConflict(index, key, modifiers)) is { } error) return error;
        return Save(index, WithGesture(binding, key, modifiers));
    }

    public string? Restore(ShortcutBinding binding)
    {
        var index = Array.IndexOf(effective, binding);
        if (index < 0 || !binding.CanCustomize) return "Для этого действия сочетание фиксировано.";
        var original = ShortcutCatalog.Bindings[index];
        if (FindConflict(index, original.Key, original.Modifiers) is { } error) return error;
        return Save(index, original);
    }

    public string? RestoreAll()
    {
        if (repository?.Save(new Dictionary<string, string>()) is { } error) return error;
        for (var index = 0; index < effective.Length; index++) effective[index] = ShortcutCatalog.Bindings[index];
        Notify();
        return null;
    }

    private string? Save(int index, ShortcutBinding updated)
    {
        var overrides = new Dictionary<string, string>();
        for (var position = 0; position < effective.Length; position++)
        {
            var binding = position == index ? updated : effective[position];
            var original = ShortcutCatalog.Bindings[position];
            if (binding.Key != original.Key || binding.Modifiers != original.Modifiers)
                overrides.Add(SettingId(position), $"{binding.Key}|{binding.Modifiers}");
        }
        if (repository?.Save(overrides) is { } error) return error;
        effective[index] = updated;
        Notify();
        return null;
    }

    private static string? ValidateGesture(ShortcutBinding binding, Key key, ModifierKeys modifiers)
    {
        const ModifierKeys knownModifiers = ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift;
        if (!Enum.IsDefined(key) || key is Key.None or Key.System or Key.ImeProcessed or Key.DeadCharProcessed or
            Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or
            Key.LWin or Key.RWin or Key.Escape || (modifiers & ~knownModifiers) != 0)
            return "Нажмите обычную клавишу или сочетание с Ctrl, Alt или Shift.";

        if (modifiers == ModifierKeys.Alt && key is Key.F4 or Key.Space ||
            modifiers is ModifierKeys.None or ModifierKeys.Shift && key == Key.Tab ||
            modifiers == ModifierKeys.None && key is Key.Enter or Key.Space)
            return "Это сочетание используется для управления окном.";
        if ((binding.AllowText || binding.CodeOnly) && key is not (>= Key.F1 and <= Key.F24))
        {
            if ((modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == 0)
                return "Для команды в поле ввода используйте Ctrl, Alt или функциональную клавишу.";
            if (modifiers == ModifierKeys.Control && key is Key.A or Key.C or Key.V or Key.X or Key.Y or Key.Z ||
                modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.Z)
                return "Это стандартное сочетание для редактирования текста.";
        }
        return null;
    }

    private string? FindConflict(int index, Key key, ModifierKeys modifiers)
    {
        var binding = effective[index];
        for (var otherIndex = 0; otherIndex < effective.Length; otherIndex++)
        {
            if (otherIndex == index) continue;
            var other = effective[otherIndex];
            if (other.Key == key && other.Modifiers == modifiers && CanOverlap(binding, other))
                return "Сочетание уже назначено действию «" + other.Description + "».";
        }
        return null;
    }

    private static bool CanOverlap(ShortcutBinding first, ShortcutBinding second) =>
        Both(first, second, false, true, false, false) || Both(first, second, false, false, false, false) ||
        Both(first, second, true, false, false, false) || Both(first, second, true, false, true, false) ||
        Both(first, second, true, false, true, true);

    // Preserves each configurable default slot even when a command has alternate shortcuts.
    private static string SettingId(int index)
    {
        var binding = ShortcutCatalog.Bindings[index];
        return $"{binding.Id}/{binding.Key}/{binding.Modifiers}";
    }

    private static ShortcutBinding WithGesture(ShortcutBinding binding, Key key, ModifierKeys modifiers)
    {
        var modifierText = (modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "") +
            (modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : "") + (modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : "");
        var keyText = key switch
        {
            Key.Up => "↑", Key.Down => "↓", Key.Left => "←", Key.Right => "→",
            Key.OemQuestion => "/", Key.OemPlus => "=", Key.OemMinus => "−",
            _ => Converter.ConvertToInvariantString(key) ?? key.ToString()
        };
        return binding with { Key = key, Modifiers = modifiers, Keys = modifierText + keyText };
    }

    private void Notify()
    {
        OnPropertyChanged(nameof(Bindings));
        OnPropertyChanged("Item[]");
    }

    private static bool Both(ShortcutBinding first, ShortcutBinding second, bool editing, bool canvas, bool code, bool editor) =>
        first.MatchesContext(editing, canvas, code, editor) && second.MatchesContext(editing, canvas, code, editor);
}
