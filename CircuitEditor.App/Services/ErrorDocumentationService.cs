using CircuitEditor.Core.Diagnostics;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.App.Services;
public sealed class ErrorDocumentationService(IErrorHelpRepository repository) : IErrorDocumentationService
{
    public IReadOnlyList<DiagnosticArticle> Articles {get;}=repository.Load().Value??[];
    public DiagnosticArticle Resolve(string code,string message="")
    {
        var exact=Articles.FirstOrDefault(a=>a.Code==code);if(exact is not null&&code!="GENERAL")return exact;
        var inferred=message.Contains("<auto>",StringComparison.OrdinalIgnoreCase)?"CODE_AUTO_MARKERS":
            message.Contains("Версия формата",StringComparison.OrdinalIgnoreCase)?"FILE_FORMAT":
            message.Contains("библиотек",StringComparison.OrdinalIgnoreCase)?"FILE_LIBRARY":null;
        return Articles.FirstOrDefault(a=>a.Code==inferred)??Articles.FirstOrDefault(a=>a.Code=="GENERAL")
            ??new("GENERAL","Неизвестная ошибка","Подробности указаны в сообщении.","Скопируйте сообщение и проверьте действие, после которого оно появилось.",[]);
    }
}
