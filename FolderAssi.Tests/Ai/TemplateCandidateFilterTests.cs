using FolderAssi.Domain.Ai;
using FolderAssi.Infrastructure.Ai;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Ai;

public sealed class TemplateCandidateFilterTests
{
    private readonly TemplateCandidateFilter _filter = new();

    [Fact]
    public void Filter_WithSpringInput_IncludesSpringTemplate()
    {
        var result = _filter.Filter(new CandidateFilterRequest
        {
            UserInput = "Spring Boot API with JWT and Swagger",
            Templates = TestTemplateFactory.CreateCoreTemplateSet().ToList()
        });

        Assert.Contains(result.Candidates, c => c.Id == "spring-boot-layered-api-starter");
    }

    [Fact]
    public void Filter_WithReactInput_IncludesReactTemplate()
    {
        var result = _filter.Filter(new CandidateFilterRequest
        {
            UserInput = "React frontend app with TypeScript",
            Templates = TestTemplateFactory.CreateCoreTemplateSet().ToList()
        });

        Assert.Contains(result.Candidates, c => c.Id == "react-feature-based-starter");
    }

    [Fact]
    public void Filter_WithUnclearInput_ReturnsConservativeFallbackCount()
    {
        var templates = TestTemplateFactory.CreateCoreTemplateSet()
            .Concat(TestTemplateFactory.CreateCoreTemplateSet())
            .Select((t, i) => t with { Id = $"{t.Id}-{i}" })
            .ToList();

        var result = _filter.Filter(new CandidateFilterRequest
        {
            UserInput = "아무거나 추천해줘",
            Templates = templates
        });

        Assert.InRange(result.Candidates.Count, 1, 3);
    }
}
