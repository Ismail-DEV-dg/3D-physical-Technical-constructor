using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CircuitEditor.App.Converters;

public sealed class ToolbarLabelVisibilityConverter : IValueConverter
{
    private const double MinimumWidth = 620;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double width && width >= MinimumWidth ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
