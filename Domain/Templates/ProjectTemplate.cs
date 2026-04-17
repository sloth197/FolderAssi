namespace FolderAssi.Domain.Templates;

public sealed record class ProjectTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Language { get; init; }
    public required string Framework { get; init; }
    public required string TemplateVersion { get; init; }
    public List<string> Tags { get; init; } = [];
    public Dictionary<string, string> DefaultVariables { get; init; } = new(StringComparer.Ordinal);
    public List<string> RequiredVariables { get; init; } = [];
    public List<TemplateOption> Options { get; init; } = [];
    public required TemplateNode Root { get; init; }
}
