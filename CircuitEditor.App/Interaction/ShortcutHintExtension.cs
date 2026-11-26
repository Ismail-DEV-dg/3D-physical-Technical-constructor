using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using CircuitEditor.App.Services;
namespace CircuitEditor.App.Interaction;
public sealed class ShortcutHintExtension(string id) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        Application.Current?.TryFindResource("ShortcutSettings") is IShortcutSettings settings
            ? new Binding($"[{id}]") { Source = settings, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider)
            : ShortcutCatalog.Hint(id);
}
