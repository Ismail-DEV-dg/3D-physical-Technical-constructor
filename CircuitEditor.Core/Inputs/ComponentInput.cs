namespace CircuitEditor.Core.Inputs;

/// <summary>Редактируемые входные условия, пока не участвующие в электрических расчётах.</summary>
public enum ComponentInput
{
    /// <summary>Температура в градусах Цельсия.</summary>
    Temperature,
    /// <summary>Относительная влажность, проценты.</summary>
    Humidity,
    /// <summary>Расстояние, сантиметры.</summary>
    Distance,
    /// <summary>Освещённость, относительные проценты.</summary>
    Illuminance,
    /// <summary>Наличие движения, 0 или 1.</summary>
    Motion,
    /// <summary>Общий вывод RGB: 0 — катод, 1 — анод.</summary>
    RgbCommon,
    /// <summary>Положение переключателя: 0 — A, 1 — B.</summary>
    SwitchPosition,
    /// <summary>Выбранное напряжение батареи, вольты.</summary>
    BatteryVoltage
}
