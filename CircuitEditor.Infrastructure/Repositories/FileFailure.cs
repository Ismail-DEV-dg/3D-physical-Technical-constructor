using System.Text.Json;

namespace CircuitEditor.Infrastructure.Repositories;

internal static class FileFailure
{
    public static string Describe(Exception error) => error switch
    {
        UnauthorizedAccessException => "Нет разрешения на доступ к файлу или папке.",
        DirectoryNotFoundException => "Папка не найдена.",
        FileNotFoundException => "Файл не найден.",
        PathTooLongException => "Имя файла или путь слишком длинный.",
        IOException when (error.HResult & 0xffff) is 32 or 33 => "Файл занят другой программой. Закройте её и повторите попытку.",
        IOException when (error.HResult & 0xffff) is 39 or 112 => "На диске недостаточно места. Освободите место и повторите попытку.",
        IOException => "Не удалось получить доступ к файлу. Проверьте, что выбран обычный файл и папка доступна.",
        JsonException or InvalidOperationException or NullReferenceException or OverflowException => "Файл повреждён или имеет неподходящий формат.",
        NotSupportedException => "Тип данных или путь не поддерживается.",
        ArgumentException => error.Message,
        _ => "Не удалось выполнить файловую операцию."
    };
}
