namespace CircuitEditor.Core.Models;

/// <summary>Физическое исполнение конденсатора с общим двухконтактным посадочным местом.</summary>
public enum CapacitorKind
{
    /// <summary>Неполярный керамический диск.</summary>
    Ceramic,
    /// <summary>Полярный электролитический цилиндр; вывод 1 — плюс, 2 — минус.</summary>
    Electrolytic
}
