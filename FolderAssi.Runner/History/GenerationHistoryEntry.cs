internal sealed record class GenerationHistoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string UserInput { get; init; } = string.Empty;
    public string SelectedTemplateId { get; init; } = string.Empty;
    public Dictionary<string, string> Variables { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, object?> Options { get; init; } = new(StringComparer.Ordinal);
    public string GeneratedProjectPath { get; init; } = string.Empty;
    public string GeneratedZipPath { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string FailureReason { get; init; } = string.Empty;
}
