namespace FolderAssi.Domain.Templates;

public sealed record class TemplateOption
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public object? Default { get; init; }
}
