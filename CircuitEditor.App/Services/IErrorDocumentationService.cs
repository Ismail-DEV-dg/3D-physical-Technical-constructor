using CircuitEditor.Core.Diagnostics;
namespace CircuitEditor.App.Services;
public interface IErrorDocumentationService
{
    IReadOnlyList<DiagnosticArticle> Articles {get;}
    DiagnosticArticle Resolve(string code,string message="");
}
