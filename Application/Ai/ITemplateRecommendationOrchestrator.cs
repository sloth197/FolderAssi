using FolderAssi.Domain.Ai;

namespace FolderAssi.Application.Ai;

public interface ITemplateRecommendationOrchestrator
{
    Task<TemplateRecommendationOrchestrationResult> RecommendAsync(
        string userInput,
        CancellationToken cancellationToken = default);
}
