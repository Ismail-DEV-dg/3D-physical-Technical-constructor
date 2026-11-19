using System.Text.RegularExpressions;
namespace CircuitEditor.Core.Code;
/// <summary>Правила имён скетчей Arduino и переносимые ограничения файловой системы.</summary>
public static class SketchName
{
    /// <summary>Предельная длина имени без расширения.</summary>
    public const int MaximumLength=63;
    /// <summary>Проверяет имя папки/основного .ino без обращения к хранилищу.</summary>
    public static bool IsValid(string name)=>name.Length<=MaximumLength
        && Regex.IsMatch(name,@"^[A-Za-z0-9_][A-Za-z0-9_.-]*$")&&!name.EndsWith('.')
        && !Regex.IsMatch(name,@"^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\.|$)",RegexOptions.IgnoreCase);
}
