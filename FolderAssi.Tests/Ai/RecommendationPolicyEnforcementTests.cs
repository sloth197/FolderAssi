using FolderAssi.Application.Ai;
using FolderAssi.Application.Templates;
using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Templates;
using FolderAssi.Infrastructure.Ai;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Ai;

public sealed class RecommendationPolicyEnforcementTests
{
    [Fact]
    public async Task RecommendAsync_WithHighConfidence_SetsAutoApprovedAndCanProceedTrue()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        var orchestrator = CreateOrchestrator(template, 0.90d, validOutput: true);

        var result = await orchestrator.RecommendAsync("asp.net web api");

        Assert.Equal(RecommendationConfidencePolicy.AutoApproved, result.ConfidencePolicy);
        Assert.True(result.Validation.IsValid);
        Assert.True(result.CanProceed);
    }

    [Fact]
    public async Task RecommendAsync_WithMidConfidence_RequiresUserConfirmationAndBlocksAutoProceed()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        var orchestrator = CreateOrchestrator(template, 0.70d, validOutput: true);

        var result = await orchestrator.RecommendAsync("asp.net web api");

        Assert.Equal(RecommendationConfidencePolicy.UserConfirmationRequired, result.ConfidencePolicy);
        Assert.True(result.Validation.IsValid);
        Assert.False(result.CanProceed);
    }

    [Fact]
    public async Task RecommendAsync_WithLowConfidence_RequiresManualSelectionAndBlocksAutoProceed()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        var orchestrator = CreateOrchestrator(template, 0.50d, validOutput: true);

        var result = await orchestrator.RecommendAsync("asp.net web api");

        Assert.Equal(RecommendationConfidencePolicy.ManualSelectionRequired, result.ConfidencePolicy);
        Assert.True(result.Validation.IsValid);
        Assert.False(result.CanProceed);
    }

    [Fact]
    public async Task RecommendAsync_EvenWithHighConfidence_WhenValidationFails_BlocksAutoProceed()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate() with
        {
            DefaultVariables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyApi"
            }
        };
        var orchestrator = CreateOrchestrator(template, 0.95d, validOutput: false);

        var result = await orchestrator.RecommendAsync("asp.net web api");

        Assert.Equal(RecommendationConfidencePolicy.AutoApproved, result.ConfidencePolicy);
        Assert.False(result.Validation.IsValid);
        Assert.False(result.CanProceed);
    }

    private static TemplateRecommendationOrchestrator CreateOrchestrator(
        ProjectTemplate template,
        double confidence,
        bool validOutput)
    {
        var loader = new StubTemplateLoader([template]);
        ITemplateCandidateFilter filter = new TemplateCandidateFilter();
        IAiTemplateRecommender recommender = new StubAiTemplateRecommender(template, confidence, validOutput);
        IAiOutputValidator validator = new AiOutputValidator();

        return new TemplateRecommendationOrchestrator(loader, filter, recommender, validator);
    }

    private sealed class StubTemplateLoader : ITemplateLoader
    {
        private readonly IReadOnlyList<ProjectTemplate> _templates;

        public StubTemplateLoader(IReadOnlyList<ProjectTemplate> templates)
        {
            _templates = templates;
        }

        public IReadOnlyList<ProjectTemplate> LoadAll()
        {
            return _templates;
        }

        public ProjectTemplate GetById(string templateId)
        {
            return _templates.Single(t => t.Id == templateId);
        }
    }

    private sealed class StubAiTemplateRecommender : IAiTemplateRecommender
    {
        private readonly ProjectTemplate _template;
        private readonly double _confidence;
        private readonly bool _validOutput;

        public StubAiTemplateRecommender(ProjectTemplate template, double confidence, bool validOutput)
        {
            _template = template;
            _confidence = confidence;
            _validOutput = validOutput;
        }

        public Task<TemplateRecommendationResult> RecommendAsync(
            TemplateRecommendationRequest request,
            CancellationToken cancellationToken = default)
        {
            var variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyApi",
                ["namespace"] = _validOutput ? "MyApi" : string.Empty
            };

            var options = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["includeAuth"] = true
            };

            var result = new TemplateRecommendationResult
            {
                TemplateId = _template.Id,
                Variables = variables,
                Options = options,
                Confidence = _confidence
            };

            return Task.FromResult(result);
        }
    }
}
