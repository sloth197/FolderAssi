using FolderAssi.Domain.Templates;

namespace FolderAssi.Application.Templates;

public interface ITemplateRenderer
{
    TemplateNode Render(
        ProjectTemplate template,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, object?> options);
}
