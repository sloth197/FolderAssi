namespace FolderAssi.Domain.Ai;

public sealed record class TemplateRecommendationResult
{
    public required string TemplateId { get; init; }
    public Dictionary<string, string> Variables { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, object?> Options { get; init; } = new(StringComparer.Ordinal);
    public double Confidence { get; init; }
    public List<string> Notes { get; init; } = [];
}
