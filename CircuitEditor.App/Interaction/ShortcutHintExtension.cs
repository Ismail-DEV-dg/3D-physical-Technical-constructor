using System.Windows.Markup;
namespace CircuitEditor.App.Interaction;
public sealed class ShortcutHintExtension(string id) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)=>ShortcutCatalog.Hint(id);
}
