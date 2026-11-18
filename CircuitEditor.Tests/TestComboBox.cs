using System.Windows.Controls;

namespace CircuitEditor.Tests;

internal sealed class TestComboBox : ComboBox
{
    public void FinishSelection() => OnDropDownClosed(EventArgs.Empty);
}
