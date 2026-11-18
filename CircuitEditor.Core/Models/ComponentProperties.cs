using System.Globalization;

namespace CircuitEditor.Core.Models;

/// <summary>Проверка редактируемых свойств компонента без зависимости от UI.</summary>
public static class ComponentProperties
{
    /// <summary>Минимальный незарезервированный семибитный I2C-адрес.</summary>
    public const int MinimumI2cAddress = 0x08;
    /// <summary>Максимальный незарезервированный семибитный I2C-адрес.</summary>
    public const int MaximumI2cAddress = 0x77;
    private const int ColorTextLength = 7;
    /// <summary>Проверяет шестизначный RGB-цвет.</summary>
    public static bool IsColor(string? value) => value is { Length: ColorTextLength }
        && value[0] == '#' && value.AsSpan(1).ToString().All(Uri.IsHexDigit);
    /// <summary>Проверяет hex-адрес, пустое значение обозначает отсутствие I2C.</summary>
    public static bool IsI2cAddress(string? value)
    {
        if (value is null) return true;
        if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || value.Length != 4) return false;
        return int.TryParse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number)
            && number is >= MinimumI2cAddress and <= MaximumI2cAddress;
    }
}
