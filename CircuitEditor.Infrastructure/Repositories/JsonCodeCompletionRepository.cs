using CircuitEditor.Core.Code;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class JsonCodeCompletionRepository : ICodeCompletionRepository
{
    public RepositoryResult<IReadOnlyList<CodeCompletionItem>> Load()
    {
        try
        {
            var data=CodeJsonReader.Read<CodeCompletionItem[]>("arduino-api.json");
            if(data.Length==0 || data.Any(i=>string.IsNullOrWhiteSpace(i.Text)||i.PinArguments is null||i.PinArguments.Any(p=>p<0))
                || data.Select(i=>i.Text).Distinct().Count()!=data.Length)throw new ArgumentException("Повреждён словарь Arduino API.");
            return new(data,null);
        }
        catch(Exception e) when(JsonProjectRepository.IsDataError(e)){return new(null,e.Message);}
    }
}
