using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.UseCases;

public class SearchKnowledgeUseCase
{
    private readonly IKnowledgeRetriever _retriever;

    public SearchKnowledgeUseCase(IKnowledgeRetriever retriever)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
    }

    public async Task<IReadOnlyList<SearchResultDto>> ExecuteAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
    {
        if (query == null || string.IsNullOrWhiteSpace(query.QueryText))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { nameof(query.QueryText), new[] { "Query text cannot be empty." } }
            });

        var searchResults = await _retriever.SearchKnowledgeAsync(query, cancellationToken);

        // Security / Business Rule (Master Spec Section 8 & 13):
        // Grounding answers requires Approved == true AND Active == true. Filter out any unapproved/inactive results.
        return searchResults
            .Where(r => r.Approved && r.Active)
            .ToList();
    }
}
