using System.Collections.Generic;
using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Templates;
using FolderAssi.Domain.Validation;

namespace FolderAssi.Application.Ai;

public interface IAiOutputValidator
{
    ValidationResult Validate(
        TemplateRecommendationResult result,
        IReadOnlyCollection<ProjectTemplate> templates);
}
