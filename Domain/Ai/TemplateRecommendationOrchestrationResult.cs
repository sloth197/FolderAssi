using FolderAssi.Domain.Validation;

namespace FolderAssi.Domain.Ai;

public enum RecommendationConfidencePolicy
{
    AutoApproved = 1,
    UserConfirmationRequired = 2,
    ManualSelectionRequired = 3
}

public sealed record class TemplateRecommendationOrchestrationResult
{
    public TemplateRecommendationResult? Recommendation { get; init; }
    public List<TemplateCandidate> Candidates { get; init; } = [];
    public ValidationResult Validation { get; init; } = ValidationResult.Success();
    public RecommendationConfidencePolicy ConfidencePolicy { get; init; } =
        RecommendationConfidencePolicy.ManualSelectionRequired;
    public string Message { get; init; } = string.Empty;

    public bool CanProceed =>
        Recommendation is not null
        && Validation.IsValid
        && ConfidencePolicy == RecommendationConfidencePolicy.AutoApproved;
}
