namespace FolderAssi.Domain.Ai;

public sealed record class TemplateRecommendationRequest
{
    public required string UserInput { get; init; }
    public List<TemplateCandidate> Candidates { get; init; } = [];
}
