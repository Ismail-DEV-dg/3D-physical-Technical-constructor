namespace CircuitEditor.Core.Models;

/// <summary>Электрическая функция вывода; дополнительные функции перечисляются в Capabilities.</summary>
public enum PinType
{
    Power, Ground, DigitalIO, AnalogIn, PWM, I2C_SDA, I2C_SCL, SPI, UART_TX, UART_RX, Passive
}
