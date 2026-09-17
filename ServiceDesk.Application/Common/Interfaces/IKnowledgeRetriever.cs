using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IKnowledgeRetriever
{
    Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default);
}
