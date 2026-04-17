using FolderAssi.Domain.Templates;

namespace FolderAssi.Domain.Ai;

public sealed record class CandidateFilterRequest
{
    public required string UserInput { get; init; }
    public List<ProjectTemplate> Templates { get; init; } = [];
}
