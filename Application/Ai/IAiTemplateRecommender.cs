using FolderAssi.Domain.Ai;

namespace FolderAssi.Application.Ai;

public interface IAiTemplateRecommender
{
    Task<TemplateRecommendationResult> RecommendAsync(
        TemplateRecommendationRequest request,
        CancellationToken cancellationToken = default);
}
