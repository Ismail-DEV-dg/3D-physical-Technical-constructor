using System.Globalization;
using System.Windows.Data;

namespace CircuitEditor.App.Converters;

public sealed class PaneLimitConverter : IMultiValueConverter
{
    private const double LeftMinimum = 180, RightMinimum = 240, CanvasMinimumWidth = 320;
    private const double LeftMaximum = 500, RightMaximum = 650, RailSize = 28, HorizontalGaps = 12;
    private const double CanvasMinimumHeight = 160, VerticalGap = 6, BottomMaximum = 420;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not double available) return BottomMaximum;
        if (parameter is "Bottom") return Math.Max(0, Math.Min(BottomMaximum, available - CanvasMinimumHeight - VerticalGap));
        if (values.Length < 7 || values[1] is not double leftSize || values[2] is not double rightSize
            || values[3] is not bool leftExpanded || values[4] is not bool rightExpanded
            || values[5] is not bool leftVisible || values[6] is not bool rightVisible) return RightMaximum;
        var leftMin = leftExpanded ? LeftMinimum : leftVisible ? RailSize : 0;
        var rightMin = rightExpanded ? RightMinimum : rightVisible ? RailSize : 0;
        var left = leftExpanded ? Math.Clamp(leftSize, LeftMinimum, LeftMaximum) : leftMin;
        var right = rightExpanded ? Math.Clamp(rightSize, RightMinimum, RightMaximum) : rightMin;
        var budget = Math.Max(leftMin + rightMin, available - CanvasMinimumWidth - HorizontalGaps);
        if (left + right <= budget) return parameter is "Left" ? LeftMaximum : RightMaximum;
        var flexible = left + right - leftMin - rightMin;
        var ratio = flexible == 0 ? 0 : (budget - leftMin - rightMin) / flexible;
        return parameter is "Left" ? leftMin + (left - leftMin) * ratio : rightMin + (right - rightMin) * ratio;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
