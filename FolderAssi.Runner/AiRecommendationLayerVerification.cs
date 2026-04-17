using FolderAssi.Application.Ai;
using FolderAssi.Application.Templates;
using FolderAssi.Domain.Ai;
using FolderAssi.Infrastructure.Ai;
using FolderAssi.Infrastructure.Templates;

internal static class AiRecommendationLayerVerification
{
    public static void Run(string templatesPath)
    {
        Console.WriteLine("== AI Recommendation Layer Verification ==");

        var failed = 0;

        ITemplateLoader templateLoader = new JsonTemplateLoader(templatesPath);
        var templates = templateLoader.LoadAll();

        var candidateFilter = new TemplateCandidateFilter();
        IAiTemplateRecommender mockRecommender = new MockAiTemplateRecommender();
        IAiOutputValidator outputValidator = new AiOutputValidator();
        var parser = new AiRecommendationParser();

        failed += TestCandidateFilter(candidateFilter, templates);
        failed += TestMockRecommender(mockRecommender, templates);
        failed += TestOutputValidator(outputValidator, templates);
        failed += TestParser(parser);

        Console.WriteLine(failed == 0
            ? "AI layer verification: PASS"
            : $"AI layer verification: FAIL ({failed} failed checks)");

        if (failed > 0)
        {
            throw new InvalidOperationException("AI recommendation layer verification failed.");
        }
    }

    private static int TestCandidateFilter(
        ITemplateCandidateFilter filter,
        IReadOnlyList<FolderAssi.Domain.Templates.ProjectTemplate> templates)
    {
        Console.WriteLine("[1] CandidateFilter");
        var failed = 0;

        var springResult = filter.Filter(new CandidateFilterRequest
        {
            UserInput = "spring boot java api",
            Templates = templates.ToList()
        });
        failed += Expect(
            springResult.Candidates.Any(static c =>
                string.Equals(c.Id, "spring-boot-layered-api-starter", StringComparison.Ordinal)),
            "spring input includes spring template");

        var reactResult = filter.Filter(new CandidateFilterRequest
        {
            UserInput = "react frontend feature based app",
            Templates = templates.ToList()
        });
        failed += Expect(
            reactResult.Candidates.Any(static c =>
                string.Equals(c.Id, "react-feature-based-starter", StringComparison.Ordinal)),
            "react input includes react template");

        return failed;
    }

    private static int TestMockRecommender(
        IAiTemplateRecommender recommender,
        IReadOnlyList<FolderAssi.Domain.Templates.ProjectTemplate> templates)
    {
        Console.WriteLine("[2] MockAiTemplateRecommender");
        var failed = 0;
        var candidateFilter = new TemplateCandidateFilter();

        var aspCandidates = candidateFilter.Filter(new CandidateFilterRequest
        {
            UserInput = "asp.net core web api with jwt",
            Templates = templates.ToList()
        }).Candidates;

        var aspResult = recommender.RecommendAsync(new TemplateRecommendationRequest
        {
            UserInput = "asp.net core web api with jwt",
            Candidates = aspCandidates
        }).GetAwaiter().GetResult();

        failed += Expect(
            string.Equals(aspResult.TemplateId, "aspnetcore-webapi-starter", StringComparison.Ordinal),
            "ASP.NET input recommends aspnetcore-webapi-starter");

        var springCandidates = candidateFilter.Filter(new CandidateFilterRequest
        {
            UserInput = "spring boot java backend",
            Templates = templates.ToList()
        }).Candidates;

        var springResult = recommender.RecommendAsync(new TemplateRecommendationRequest
        {
            UserInput = "spring boot java backend",
            Candidates = springCandidates
        }).GetAwaiter().GetResult();

        failed += Expect(
            string.Equals(springResult.TemplateId, "spring-boot-layered-api-starter", StringComparison.Ordinal),
            "Spring input recommends spring-boot-layered-api-starter");

        return failed;
    }

    private static int TestOutputValidator(
        IAiOutputValidator validator,
        IReadOnlyList<FolderAssi.Domain.Templates.ProjectTemplate> templates)
    {
        Console.WriteLine("[3] AiOutputValidator");
        var failed = 0;

        var unknownTemplateResult = validator.Validate(
            new TemplateRecommendationResult
            {
                TemplateId = "unknown-template",
                Variables = new Dictionary<string, string>(StringComparer.Ordinal),
                Options = new Dictionary<string, object?>(StringComparer.Ordinal),
                Confidence = 0.8d,
                Notes = []
            },
            templates);
        failed += Expect(!unknownTemplateResult.IsValid, "reject unknown templateId");

        var aspTemplate = templates.First(static t =>
            string.Equals(t.Id, "aspnetcore-webapi-starter", StringComparison.Ordinal));

        var aspWithoutDefaults = aspTemplate with
        {
            DefaultVariables = new Dictionary<string, string>(StringComparer.Ordinal)
        };

        var missingVariableResult = validator.Validate(
            new TemplateRecommendationResult
            {
                TemplateId = "aspnetcore-webapi-starter",
                Variables = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["projectName"] = "MyApi"
                },
                Options = new Dictionary<string, object?>(StringComparer.Ordinal),
                Confidence = 0.8d,
                Notes = []
            },
            [aspWithoutDefaults]);
        failed += Expect(!missingVariableResult.IsValid, "reject missing required variable");

        var unknownOptionResult = validator.Validate(
            new TemplateRecommendationResult
            {
                TemplateId = "aspnetcore-webapi-starter",
                Variables = new Dictionary<string, string>(StringComparer.Ordinal),
                Options = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["unknownOption"] = true
                },
                Confidence = 0.8d,
                Notes = []
            },
            templates);
        failed += Expect(!unknownOptionResult.IsValid, "reject undefined option key");

        var badConfidenceResult = validator.Validate(
            new TemplateRecommendationResult
            {
                TemplateId = "aspnetcore-webapi-starter",
                Variables = new Dictionary<string, string>(StringComparer.Ordinal),
                Options = new Dictionary<string, object?>(StringComparer.Ordinal),
                Confidence = 1.4d,
                Notes = []
            },
            templates);
        failed += Expect(!badConfidenceResult.IsValid, "reject confidence out of range");

        return failed;
    }

    private static int TestParser(AiRecommendationParser parser)
    {
        Console.WriteLine("[4] AiRecommendationParser");
        var failed = 0;

        const string validJson = """
{
  "templateId": "aspnetcore-webapi-starter",
  "variables": { "projectName": "MyApi", "namespace": "MyApi" },
  "options": { "includeAuth": true },
  "confidence": 0.9,
  "notes": ["ok"]
}
""";

        try
        {
            var parsed = parser.Parse(validJson);
            failed += Expect(
                string.Equals(parsed.TemplateId, "aspnetcore-webapi-starter", StringComparison.Ordinal),
                "parse valid JSON");
        }
        catch
        {
            failed += Expect(false, "parse valid JSON");
        }

        const string invalidJson = """
{
  "templateId": "aspnetcore-webapi-starter",
  "variables": { "projectName": "MyApi" },
  "options": { "includeAuth": true },
  "confidence": 0.9,
  "notes": ["ok"]
this is broken
}
""";

        try
        {
            _ = parser.Parse(invalidJson);
            failed += Expect(false, "reject invalid JSON");
        }
        catch
        {
            failed += Expect(true, "reject invalid JSON");
        }

        return failed;
    }

    private static int Expect(bool condition, string label)
    {
        Console.WriteLine($"- {label}: {(condition ? "PASS" : "FAIL")}");
        return condition ? 0 : 1;
    }
}
