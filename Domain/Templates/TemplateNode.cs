namespace FolderAssi.Domain.Templates;

public sealed record class TemplateNode
{
    public required string Name { get; init; }
    public required TemplateNodeType Type { get; init; }
    public string? Description { get; init; }
    public bool Optional { get; init; }
    public string? ConditionKey { get; init; }
    public List<TemplateNode> Children { get; init; } = [];
    public string? ContentTemplate { get; init; }
    public string Encoding { get; init; } = "utf-8";
    public string OverwritePolicy { get; init; } = "error";
    public bool IsBinary { get; init; }
}
