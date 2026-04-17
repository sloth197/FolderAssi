using System.Text.RegularExpressions;
using FolderAssi.Application.Ai;
using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Templates;

namespace FolderAssi.Infrastructure.Ai;

public sealed class TemplateCandidateFilter : ITemplateCandidateFilter
{
    private const int MaxCandidates = 5;
    private const int FallbackCandidates = 3;

    public CandidateFilterResult Filter(CandidateFilterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            throw new ArgumentException("UserInput is required.", nameof(request));
        }

        if (request.Templates.Count == 0)
        {
            throw new ArgumentException("At least one template is required.", nameof(request));
        }

        var normalizedInput = request.UserInput.Trim().ToLowerInvariant();
        var tokens = Tokenize(normalizedInput);

        var scored = request.Templates
            .Select(template => new
            {
                Candidate = ToCandidate(template),
                Score = ScoreTemplate(template, normalizedInput, tokens)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Candidate.Id, StringComparer.Ordinal)
            .Take(MaxCandidates)
            .Select(item => item.Candidate)
            .ToList();

        if (scored.Count == 0)
        {
            scored = request.Templates
                .OrderBy(GetFallbackRank)
                .ThenBy(template => template.Id, StringComparer.Ordinal)
                .Take(Math.Min(FallbackCandidates, request.Templates.Count))
                .Select(ToCandidate)
                .ToList();
        }

        return new CandidateFilterResult
        {
            Candidates = scored
        };
    }

    private static int ScoreTemplate(
        ProjectTemplate template,
        string normalizedInput,
        IReadOnlySet<string> tokens)
    {
        var score = 0;

        if (ContainsSpecializedIntent(normalizedInput, "spring", "java", "boot")
            && ContainsAny(template, "spring", "java"))
        {
            score += 8;
        }

        if (ContainsSpecializedIntent(normalizedInput, "asp", "asp.net", "aspnet", "c#", ".net", "web api")
            && ContainsAny(template, "asp", "asp.net", "aspnet", "c#", ".net"))
        {
            score += 8;
        }

        if (ContainsSpecializedIntent(normalizedInput, "react", "frontend", "front-end")
            && ContainsAny(template, "react", "frontend"))
        {
            score += 8;
        }

        if (ContainsWord(normalizedInput, template.Language))
        {
            score += 4;
        }

        if (ContainsWord(normalizedInput, template.Framework))
        {
            score += 4;
        }

        if (ContainsWord(normalizedInput, "backend") && template.Tags.Contains("backend", StringComparer.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (ContainsWord(normalizedInput, "frontend") && template.Tags.Contains("frontend", StringComparer.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (ContainsWord(normalizedInput, "api") && template.Tags.Contains("api", StringComparer.OrdinalIgnoreCase))
        {
            score += 2;
        }

        foreach (var tag in template.Tags)
        {
            if (tokens.Contains(tag.ToLowerInvariant()))
            {
                score += 1;
            }
        }

        return score;
    }

    private static int GetFallbackRank(ProjectTemplate template)
    {
        if (template.Tags.Contains("backend", StringComparer.OrdinalIgnoreCase)
            || template.Tags.Contains("frontend", StringComparer.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (template.Tags.Contains("api", StringComparer.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }

    private static TemplateCandidate ToCandidate(ProjectTemplate template)
    {
        return new TemplateCandidate
        {
            Id = template.Id,
            Name = template.Name,
            Language = template.Language,
            Framework = template.Framework,
            Tags = template.Tags.ToList(),
            RequiredVariables = template.RequiredVariables.ToList(),
            SupportedOptionKeys = template.Options.Select(option => option.Key).ToList()
        };
    }

    private static bool ContainsAny(ProjectTemplate template, params string[] keywords)
    {
        var haystack = string.Join(
            " ",
            template.Id,
            template.Name,
            template.Language,
            template.Framework,
            string.Join(" ", template.Tags));

        return keywords.Any(keyword => ContainsWord(haystack, keyword));
    }

    private static bool ContainsSpecializedIntent(string input, params string[] keywords)
    {
        return keywords.Any(keyword => ContainsWord(input, keyword));
    }

    private static bool ContainsWord(string input, string? word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return input.ToLowerInvariant().Contains(word.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    private static IReadOnlySet<string> Tokenize(string input)
    {
        var matches = Regex.Matches(input, @"[a-z0-9\.\-\+#]+", RegexOptions.CultureInvariant);
        var tokens = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in matches)
        {
            if (!string.IsNullOrWhiteSpace(match.Value))
            {
                tokens.Add(match.Value);
            }
        }

        return tokens;
    }
}
