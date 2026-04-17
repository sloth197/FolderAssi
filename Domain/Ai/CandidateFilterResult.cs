namespace FolderAssi.Domain.Ai;

public sealed record class CandidateFilterResult
{
    public List<TemplateCandidate> Candidates { get; init; } = [];
}
