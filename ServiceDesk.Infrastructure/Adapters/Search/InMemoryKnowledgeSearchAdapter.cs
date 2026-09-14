using System.Collections.Concurrent;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Infrastructure.Adapters.Search;

public class InMemoryKnowledgeSearchAdapter : IKnowledgeRetriever, IKnowledgeIndexStore
{
    private readonly ConcurrentDictionary<string, KnowledgeDocumentDto> _chunks = new();

    public Task UpsertChunkAsync(KnowledgeDocumentDto chunk, CancellationToken cancellationToken = default)
    {
        _chunks[chunk.Id] = chunk;
        return Task.CompletedTask;
    }

    public Task UpsertChunksBatchAsync(IEnumerable<KnowledgeDocumentDto> chunks, CancellationToken cancellationToken = default)
    {
        foreach (var chunk in chunks)
        {
            _chunks[chunk.Id] = chunk;
        }
        return Task.CompletedTask;
    }

    public Task<int> GetIndexedCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_chunks.Count);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _chunks.Clear();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.QueryText))
        {
            return Task.FromResult<IReadOnlyList<SearchResultDto>>(Array.Empty<SearchResultDto>());
        }

        var terms = query.QueryText.Split(new[] { ' ', '\t', '\r', '\n', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 1)
            .Distinct()
            .ToList();

        if (terms.Count == 0)
        {
            terms.Add(query.QueryText.Trim().ToLowerInvariant());
        }

        var candidateList = _chunks.Values.AsEnumerable();

        if (query.FilterApprovedOnly)
        {
            candidateList = candidateList.Where(doc => doc.Approved && doc.Active);
        }

        var results = candidateList
            .Select(doc =>
            {
                double score = CalculateRelevanceScore(doc, query.QueryText, terms);
                return new
                {
                    Document = doc,
                    Score = score
                };
            })
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Document.DocumentName)
            .Take(query.TopK)
            .Select(r => new SearchResultDto(
                DocumentName: r.Document.DocumentName,
                Version: r.Document.Version,
                Section: r.Document.Section,
                Page: r.Document.Page,
                Content: r.Document.Content,
                Score: Math.Round(r.Score, 4),
                Approved: r.Document.Approved,
                Active: r.Document.Active,
                BlobPath: r.Document.BlobPath
            ))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResultDto>>(results);
    }

    private static double CalculateRelevanceScore(KnowledgeDocumentDto doc, string fullQuery, List<string> terms)
    {
        double score = 0.0;
        string lowerContent = doc.Content.ToLowerInvariant();
        string lowerDocName = doc.DocumentName.ToLowerInvariant();
        string lowerSection = doc.Section.ToLowerInvariant();
        string lowerQuery = fullQuery.ToLowerInvariant();

        // Exact phrase match bonus
        if (lowerContent.Contains(lowerQuery))
        {
            score += 5.0;
        }

        if (lowerDocName.Contains(lowerQuery) || lowerSection.Contains(lowerQuery))
        {
            score += 3.0;
        }

        // Term matching score
        foreach (var term in terms)
        {
            if (lowerDocName.Contains(term))
            {
                score += 2.0;
            }

            if (lowerSection.Contains(term))
            {
                score += 1.5;
            }

            int occurrences = CountOccurrences(lowerContent, term);
            if (occurrences > 0)
            {
                score += 1.0 + Math.Min(occurrences * 0.2, 2.0);
            }
        }

        return score;
    }

    private static int CountOccurrences(string source, string term)
    {
        if (string.IsNullOrEmpty(term) || string.IsNullOrEmpty(source)) return 0;
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(term, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += term.Length;
        }
        return count;
    }
}
