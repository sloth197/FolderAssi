namespace FolderAssi.Domain.Ai;

public sealed record class TemplateCandidate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Language { get; init; } = string.Empty;
    public string Framework { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public List<string> RequiredVariables { get; init; } = [];
    public List<string> SupportedOptionKeys { get; init; } = [];
}
