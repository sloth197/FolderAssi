using FolderAssi.Domain.Templates;
using FolderAssi.Domain.Validation;

namespace FolderAssi.Application.Templates;

public interface ITemplateValidator
{
    ValidationResult Validate(ProjectTemplate template);
}