using FolderAssi.Application.Ai;
using FolderAssi.Application.Templates;
using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Validation;

namespace FolderAssi.Infrastructure.Ai;

public sealed class TemplateRecommendationOrchestrator : ITemplateRecommendationOrchestrator
{
    private readonly ITemplateLoader _templateLoader;
    private readonly ITemplateCandidateFilter _candidateFilter;
    private readonly IAiTemplateRecommender _aiTemplateRecommender;
    private readonly IAiOutputValidator _aiOutputValidator;

    public TemplateRecommendationOrchestrator(
        ITemplateLoader templateLoader,
        ITemplateCandidateFilter candidateFilter,
        IAiTemplateRecommender aiTemplateRecommender,
        IAiOutputValidator aiOutputValidator)
    {
        _templateLoader = templateLoader ?? throw new ArgumentNullException(nameof(templateLoader));
        _candidateFilter = candidateFilter ?? throw new ArgumentNullException(nameof(candidateFilter));
        _aiTemplateRecommender = aiTemplateRecommender ?? throw new ArgumentNullException(nameof(aiTemplateRecommender));
        _aiOutputValidator = aiOutputValidator ?? throw new ArgumentNullException(nameof(aiOutputValidator));
    }

    public async Task<TemplateRecommendationOrchestrationResult> RecommendAsync(
        string userInput,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        var templates = _templateLoader.LoadAll();

        var filterResult = _candidateFilter.Filter(new CandidateFilterRequest
        {
            UserInput = userInput,
            Templates = templates.ToList()
        });

        if (filterResult.Candidates.Count == 0)
        {
            var emptyValidation = ValidationResult.Success();
            emptyValidation.AddError("NO_CANDIDATES", "No template candidates are available.");

            return new TemplateRecommendationOrchestrationResult
            {
                Candidates = [],
                Validation = emptyValidation,
                ConfidencePolicy = RecommendationConfidencePolicy.ManualSelectionRequired,
                Message = "추천 가능한 템플릿 후보가 없습니다. 수동 선택이 필요합니다."
            };
        }

        var recommendationRequest = new TemplateRecommendationRequest
        {
            UserInput = userInput,
            Candidates = filterResult.Candidates
        };

        var recommendation = await _aiTemplateRecommender.RecommendAsync(
            recommendationRequest,
            cancellationToken);

        var validation = _aiOutputValidator.Validate(recommendation, templates);

        var policy = DeterminePolicy(recommendation.Confidence);
        var message = BuildMessage(policy, validation);

        return new TemplateRecommendationOrchestrationResult
        {
            Recommendation = recommendation,
            Candidates = filterResult.Candidates,
            Validation = validation,
            ConfidencePolicy = policy,
            Message = message
        };
    }

    private static RecommendationConfidencePolicy DeterminePolicy(double confidence)
    {
        if (confidence >= 0.85d)
        {
            return RecommendationConfidencePolicy.AutoApproved;
        }

        if (confidence >= 0.60d)
        {
            return RecommendationConfidencePolicy.UserConfirmationRequired;
        }

        return RecommendationConfidencePolicy.ManualSelectionRequired;
    }

    private static string BuildMessage(
        RecommendationConfidencePolicy policy,
        ValidationResult validation)
    {
        if (!validation.IsValid)
        {
            return "AI 추천 결과 검증에 실패했습니다. 프로젝트 생성을 중단합니다.";
        }

        return policy switch
        {
            RecommendationConfidencePolicy.AutoApproved =>
                "신뢰도 0.85 이상입니다. 자동 선택이 가능합니다.",
            RecommendationConfidencePolicy.UserConfirmationRequired =>
                "신뢰도 0.60 이상입니다. 사용자 확인 후 진행하세요.",
            _ => "신뢰도 0.60 미만입니다. 수동 템플릿 선택을 유도하세요."
        };
    }
}
