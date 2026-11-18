using System.Text.Json.Serialization;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Inputs;

namespace CircuitEditor.Core.Models;

/// <summary>Электрический экземпляр без координат и типов графической платформы.</summary>
public sealed class Component
{
    /// <summary>Максимальная длина номинала.</summary>
    public const int MaximumValueLength = 80;
    /// <summary>Создаёт электрический экземпляр с настройками библиотеки.</summary>
    public Component(ComponentDefinition definition, string reference)
        : this(definition, reference, Guid.NewGuid(), definition?.DefaultValue
            ?? throw new ArgumentNullException(nameof(definition)), definition.DefaultColor, definition.DefaultI2cAddress) { }
    /// <summary>Восстанавливает все электрические данные и устойчивый ID из JSON.</summary>
    [JsonConstructor]
    public Component(ComponentDefinition definition, string reference, Guid id, string value, string color, string? i2cAddress,
        CapacitorKind capacitorKind = CapacitorKind.Ceramic, double potentiometerAngle = 0, ComponentInputs? inputs = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (id == Guid.Empty) throw new ArgumentException("Пустой ID компонента.");
        Definition = definition; Reference = reference; Id = id;
        Value = definition.DefaultValue; Color = definition.DefaultColor; I2cAddress = definition.DefaultI2cAddress;
        if (!TryUpdateProperties(value, color, i2cAddress, out var error)) throw new ArgumentException(error);
        if (!Enum.IsDefined(capacitorKind) || !double.IsFinite(potentiometerAngle)
            || potentiometerAngle < PotentiometerPosition.MinimumAngle || potentiometerAngle > PotentiometerPosition.MaximumAngle
            || definition.Id != "capacitor" && capacitorKind != CapacitorKind.Ceramic
            || definition.Id != "potentiometer" && potentiometerAngle != 0)
            throw new ArgumentException("Некорректное исполнение или положение ручки.");
        CapacitorKind = capacitorKind; PotentiometerAngle = potentiometerAngle;
        Inputs = inputs ?? ComponentInputDefinitions.Defaults(definition.Id);
        if (!ComponentInputDefinitions.IsValid(definition.Id, Inputs)) throw new ArgumentException("Входные условия вне диапазона или не относятся к компоненту.");
    }
    /// <summary>Устойчивый ID.</summary>
    public Guid Id { get; }
    /// <summary>Электрическое описание типа.</summary>
    public ComponentDefinition Definition { get; }
    /// <summary>Обозначение R1, U1 и т.п.</summary>
    public string Reference { get; }
    /// <summary>Номинал экземпляра.</summary>
    public string Value { get; private set; }
    /// <summary>Цвет экземпляра.</summary>
    public string Color { get; private set; }
    /// <summary>Адрес шины I2C либо null.</summary>
    public string? I2cAddress { get; private set; }
    /// <summary>Исполнение конденсатора, сохраняемое в JSON.</summary>
    public CapacitorKind CapacitorKind { get; private set; }
    /// <summary>Угол ручки потенциометра в градусах, сохраняемый в JSON.</summary>
    public double PotentiometerAngle { get; private set; }
    /// <summary>Пользовательские условия для будущей симуляции; сейчас без влияния на электрику.</summary>
    public ComponentInputs Inputs { get; private set; }
    /// <summary>Атомарно меняет один применимый параметр после проверки.</summary>
    public bool TrySetInput(ComponentInput input, double value)
    {
        var definition = ComponentInputDefinitions.For(Definition.Id).FirstOrDefault(item => item.Input == input);
        if (definition is null || !definition.IsValid(value)) return false;
        var values = Inputs.Values.ToDictionary(pair => pair.Key, pair => pair.Value);
        values[input] = value; Inputs = new ComponentInputs(values);
        if (input is ComponentInput.BatteryVoltage or ComponentInput.RgbCommon) Value = definition.Format(value);
        return true;
    }
    /// <summary>Проверяет и меняет исполнение только у конденсатора.</summary>
    public bool TrySetCapacitorKind(CapacitorKind kind)
    {
        if (Definition.Id != "capacitor" || !Enum.IsDefined(kind)) return false;
        CapacitorKind = kind; return true;
    }
    /// <summary>Проверяет и меняет угол только у потенциометра.</summary>
    public bool TrySetPotentiometerAngle(double angle)
    {
        if (Definition.Id != "potentiometer" || !double.IsFinite(angle)
            || angle < PotentiometerPosition.MinimumAngle || angle > PotentiometerPosition.MaximumAngle) return false;
        PotentiometerAngle = angle; return true;
    }
    /// <summary>Атомарно применяет проверенные свойства.</summary>
    public bool TryUpdateProperties(string value, string color, string? address, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumValueLength)
            error = "Введите номинал длиной от 1 до 80 символов.";
        else if (!ComponentProperties.IsColor(color))
            error = "Цвет: #RRGGBB, например #39FF88.";
        else if (Definition.DefaultI2cAddress is not null && (address is null || !ComponentProperties.IsI2cAddress(address)))
            error = "I2C-адрес: от 0x08 до 0x77.";
        else if (Definition.DefaultI2cAddress is null && address is not null)
            error = "Этот компонент не поддерживает I2C.";
        if (error is not null) return false;
        Value = value.Trim(); Color = color.ToUpperInvariant();
        I2cAddress = address?.ToUpperInvariant().Replace("0X", "0x");
        return true;
    }
}
