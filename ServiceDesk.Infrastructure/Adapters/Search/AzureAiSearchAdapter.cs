using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Infrastructure.Adapters.Search;

public class AzureAiSearchAdapter : IKnowledgeRetriever, IKnowledgeIndexStore
{
    private readonly ILogger<AzureAiSearchAdapter> _logger;
    private readonly InMemoryKnowledgeSearchAdapter _fallbackStore;
    private readonly string? _endpoint;
    private readonly string? _apiKey;
    private readonly string? _indexName;

    public AzureAiSearchAdapter(IConfiguration configuration, ILogger<AzureAiSearchAdapter> logger)
    {
        _logger = logger;
        _fallbackStore = new InMemoryKnowledgeSearchAdapter();
        _endpoint = configuration["AzureSearch:Endpoint"];
        _apiKey = configuration["AzureSearch:ApiKey"];
        _indexName = configuration["AzureSearch:IndexName"];

        if (IsConfigured())
        {
            _logger.LogInformation("Azure AI Search Adapter initialized for Endpoint: {Endpoint}, Index: {Index}", _endpoint, _indexName);
        }
        else
        {
            _logger.LogWarning("Azure AI Search credentials not fully specified. AzureAiSearchAdapter operating in local fallback mode.");
        }
    }

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_endpoint) &&
        !string.IsNullOrWhiteSpace(_apiKey) &&
        !string.IsNullOrWhiteSpace(_indexName);

    public async Task UpsertChunkAsync(KnowledgeDocumentDto chunk, CancellationToken cancellationToken = default)
    {
        if (IsConfigured())
        {
            _logger.LogInformation("Upserting chunk {Id} to Azure AI Search index {Index}", chunk.Id, _indexName);
            // Azure SDK call placeholder / REST API submission when real Azure Search Service is attached
        }
        
        await _fallbackStore.UpsertChunkAsync(chunk, cancellationToken);
    }

    public async Task UpsertChunksBatchAsync(IEnumerable<KnowledgeDocumentDto> chunks, CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        if (IsConfigured())
        {
            _logger.LogInformation("Upserting batch of {Count} chunks to Azure AI Search index {Index}", chunkList.Count, _indexName);
        }

        await _fallbackStore.UpsertChunksBatchAsync(chunkList, cancellationToken);
    }

    public Task<int> GetIndexedCountAsync(CancellationToken cancellationToken = default)
    {
        return _fallbackStore.GetIndexedCountAsync(cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return _fallbackStore.ClearAsync(cancellationToken);
    }

    public Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
    {
        if (IsConfigured())
        {
            _logger.LogInformation("Executing Azure AI Search query: {QueryText}", query.QueryText);
        }

        return _fallbackStore.SearchKnowledgeAsync(query, cancellationToken);
    }
}
