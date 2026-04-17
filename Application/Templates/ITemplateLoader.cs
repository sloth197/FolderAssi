using FolderAssi.Domain.Templates;

namespace FolderAssi.Application.Templates;

public interface ITemplateLoader
{
    IReadOnlyList<ProjectTemplate> LoadAll();

    ProjectTemplate GetById(string templateId);
}
