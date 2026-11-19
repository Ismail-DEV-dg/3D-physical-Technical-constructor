using CircuitEditor.Core.Code;
using CircuitEditor.Core.Repositories;
namespace CircuitEditor.Infrastructure.Repositories;
public sealed class JsonCodeTemplateRepository : ICodeTemplateRepository
{
    public RepositoryResult<CodeCatalog> Load()
    {
        try
        {
            var data=CodeJsonReader.Read<CodeCatalog>("code-templates.json");
            if(data.SchemaVersion!=1 || data.Boards.Count!=3 || data.Boards.Select(b=>b.Kind).Distinct().Count()!=3
                || data.Boards.Any(b=>b.Pins.Count==0||b.Pins.Select(p=>p.Id).Distinct().Count()!=b.Pins.Count)
                || data.Templates.Select(t=>t.ComponentId).Distinct().Count()!=data.Templates.Count
                || data.Templates.Any(t=>t.Signals is null||t.Includes is null))throw new ArgumentException("Повреждён каталог шаблонов кода.");
            return new(data,null);
        }
        catch(Exception e) when(JsonProjectRepository.IsDataError(e)){return new(null,e.Message);}
    }
}
