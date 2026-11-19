using CircuitEditor.Core.Diagnostics;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class JsonErrorHelpRepository : IErrorHelpRepository
{
    public RepositoryResult<IReadOnlyList<DiagnosticArticle>> Load()
    {
        try
        {
            var articles=CodeJsonReader.Read<DiagnosticArticle[]>("error-help.json");
            if(articles.Length==0||articles.Any(a=>string.IsNullOrWhiteSpace(a.Code)||string.IsNullOrWhiteSpace(a.Fix)||a.Keywords is null)
                || articles.Select(a=>a.Code).Distinct().Count()!=articles.Length)throw new ArgumentException("Повреждена справка по ошибкам.");
            return new(articles,null);
        }
        catch(Exception e) when(JsonProjectRepository.IsDataError(e)){return new(null,e.Message);}
    }
}
