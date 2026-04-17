using FolderAssi.Domain.Ai;
using FolderAssi.Infrastructure.Ai;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Ai;

public sealed class MockAiTemplateRecommenderTests
{
    private readonly MockAiTemplateRecommender _recommender = new();

    [Fact]
    public async Task RecommendAsync_WithAspNetInput_ReturnsAspNetTemplate()
    {
        var request = new TemplateRecommendationRequest
        {
            UserInput = "JWT 로그인 기능이 포함된 ASP.NET Core Web API 프로젝트",
            Candidates = BuildCandidates()
        };

        var result = await _recommender.RecommendAsync(request);

        Assert.Equal("aspnetcore-webapi-starter", result.TemplateId);
        Assert.True(result.Confidence >= 0.90d);
        Assert.True(result.Options.TryGetValue("includeAuth", out var includeAuth));
        Assert.Equal(true, includeAuth);
    }

    [Fact]
    public async Task RecommendAsync_WithSpringInput_ReturnsSpringTemplate()
    {
        var request = new TemplateRecommendationRequest
        {
            UserInput = "spring boot java api",
            Candidates = BuildCandidates()
        };

        var result = await _recommender.RecommendAsync(request);

        Assert.Equal("spring-boot-layered-api-starter", result.TemplateId);
        Assert.True(result.Variables.ContainsKey("packageName"));
    }

    private static List<TemplateCandidate> BuildCandidates()
    {
        return TestTemplateFactory.CreateCoreTemplateSet()
            .Select(TestTemplateFactory.ToCandidate)
            .ToList();
    }
}
