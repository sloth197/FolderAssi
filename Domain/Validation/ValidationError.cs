namespace FolderAssi.Domain.Validation;

public sealed record class ValidationError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? Path { get; init; }
}
