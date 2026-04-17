namespace FolderAssi.Infrastructure.Ai;

public sealed record class OpenAiRecommendationOptions
{
    public required string ApiKey { get; init; }
    public required string Endpoint { get; init; }
    public required string Model { get; init; }
}
