using FolderAssi.Application.Ai;
using FolderAssi.Domain.Ai;

namespace FolderAssi.Infrastructure.Ai;

public sealed class MockAiTemplateRecommender : IAiTemplateRecommender
{
    public Task<TemplateRecommendationResult> RecommendAsync(
        TemplateRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Candidates.Count == 0)
        {
            throw new ArgumentException("At least one template candidate is required.", nameof(request));
        }

        var input = request.UserInput.Trim().ToLowerInvariant();

        var templateId = SelectTemplateId(input, request.Candidates);
        var selected = request.Candidates.First(candidate =>
            string.Equals(candidate.Id, templateId, StringComparison.Ordinal));

        var variables = BuildVariables(selected, templateId);
        var options = BuildOptions(selected, input);
        var confidence = CalculateConfidence(input, templateId);

        var result = new TemplateRecommendationResult
        {
            TemplateId = templateId,
            Variables = variables,
            Options = options,
            Confidence = confidence,
            Notes =
            [
                "mock recommender rule-based result"
            ]
        };

        return Task.FromResult(result);
    }

    private static string SelectTemplateId(string input, IReadOnlyList<TemplateCandidate> candidates)
    {
        if (ContainsAny(input, "spring", "java", "boot"))
        {
            var spring = candidates.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, "spring-boot-layered-api-starter", StringComparison.Ordinal));
            if (spring is not null)
            {
                return spring.Id;
            }
        }

        if (ContainsAny(input, "asp", "asp.net", "aspnet", "c#", ".net", "web api"))
        {
            var asp = candidates.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, "aspnetcore-webapi-starter", StringComparison.Ordinal));
            if (asp is not null)
            {
                return asp.Id;
            }
        }

        if (ContainsAny(input, "react", "frontend", "front-end"))
        {
            var react = candidates.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, "react-feature-based-starter", StringComparison.Ordinal));
            if (react is not null)
            {
                return react.Id;
            }
        }

        return candidates.OrderBy(candidate => candidate.Id, StringComparer.Ordinal).First().Id;
    }

    private static Dictionary<string, string> BuildVariables(TemplateCandidate selected, string templateId)
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var variableKey in selected.RequiredVariables)
        {
            variables[variableKey] = variableKey switch
            {
                "projectName" => templateId switch
                {
                    "spring-boot-layered-api-starter" => "MySpringApi",
                    "aspnetcore-webapi-starter" => "MyAwesomeApi",
                    "react-feature-based-starter" => "my-react-app",
                    _ => "MyProject"
                },
                "namespace" => "MyAwesomeApi",
                "packageName" => "com.mycompany.myapp",
                "mainClassName" => "Application",
                _ => "value"
            };
        }

        return variables;
    }

    private static Dictionary<string, object?> BuildOptions(TemplateCandidate selected, string input)
    {
        var options = new Dictionary<string, object?>(StringComparer.Ordinal);
        var includeAuth = ContainsAny(input, "auth", "jwt", "login", "security");
        var includeSwagger = ContainsAny(input, "swagger", "openapi");

        foreach (var key in selected.SupportedOptionKeys)
        {
            options[key] = key switch
            {
                "includeAuth" => includeAuth,
                "includeSwagger" => includeSwagger,
                _ => false
            };
        }

        return options;
    }

    private static double CalculateConfidence(string input, string templateId)
    {
        return templateId switch
        {
            "spring-boot-layered-api-starter" when ContainsAny(input, "spring", "java", "boot") => 0.92d,
            "aspnetcore-webapi-starter" when ContainsAny(input, "asp", "aspnet", ".net", "c#", "web api") => 0.92d,
            "react-feature-based-starter" when ContainsAny(input, "react", "frontend") => 0.90d,
            _ => 0.65d
        };
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        return keywords.Any(keyword => source.Contains(keyword, StringComparison.Ordinal));
    }
}
