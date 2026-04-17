using FolderAssi.Domain.Ai;

namespace FolderAssi.Application.Ai;

public interface ITemplateCandidateFilter
{
    CandidateFilterResult Filter(CandidateFilterRequest request);
}
