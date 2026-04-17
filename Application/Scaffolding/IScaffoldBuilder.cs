using FolderAssi.Domain.Templates;

namespace FolderAssi.Application.Scaffolding;

public interface IScaffoldBuilder
{
    string BuildProject(string outputRootPath, TemplateNode renderedRoot);
}
