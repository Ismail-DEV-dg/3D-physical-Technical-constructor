using CircuitEditor.Core.Geometry;

namespace CircuitEditor.Core.Layout;

/// <summary>Общие именованные метрики раскладки всех библиотечных типов.</summary>
public static class ComponentLayoutStyle
{
    /// <summary>Размер подписей при масштабе 100%.</summary>
    public const double FontSize = 12;
    /// <summary>Длина вывода снаружи корпуса; кратна сетке.</summary>
    public const double LeadLength = DiagramGeometry.GridSize;
    /// <summary>Минимальное расстояние от конца линии вывода до текста.</summary>
    public const double PinTextClearance = 8;
    /// <summary>Поля заголовков и нижних подписей внутри корпуса.</summary>
    public const double BodyPadding = 12;
    /// <summary>Расстояние между текстовыми зонами.</summary>
    public const double TextGap = 8;
    /// <summary>Зазор между колонкой подписей и символом.</summary>
    public const double SymbolGap = 12;
    /// <summary>Резерв для векторной иконки с учётом контура.</summary>
    public const double SymbolWidth = 80;
    /// <summary>Высота резерва векторной иконки с учётом лучей светодиода.</summary>
    public const double SymbolHeight = 60;
    /// <summary>Минимальный вертикальный зазор соседних подписей.</summary>
    public const double RowGap = 2;
}
