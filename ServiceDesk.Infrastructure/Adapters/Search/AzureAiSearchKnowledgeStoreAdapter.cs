using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Infrastructure.Adapters.Search;

public class AzureAiSearchKnowledgeStoreAdapter : IKnowledgeIndexStore, IKnowledgeRetriever
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _indexName;
    private readonly AzureOpenAiEmbeddingService _embeddingService;
    private readonly ILogger<AzureAiSearchKnowledgeStoreAdapter> _logger;

    private readonly SearchIndexClient? _indexClient;
    private readonly SearchClient? _searchClient;
    private readonly bool _isConfigured;
    private bool _indexEnsured;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AzureAiSearchKnowledgeStoreAdapter(
        IConfiguration configuration,
        AzureOpenAiEmbeddingService embeddingService,
        ILogger<AzureAiSearchKnowledgeStoreAdapter> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _endpoint = configuration["AzureAiSearch:Endpoint"] ?? string.Empty;
        _apiKey = configuration["AzureAiSearch:ApiKey"] ?? string.Empty;
        _indexName = configuration["AzureAiSearch:IndexName"] ?? "servicedesk-knowledge-index-v2";

        _isConfigured = !string.IsNullOrWhiteSpace(_endpoint) &&
                        !string.IsNullOrWhiteSpace(_apiKey) &&
                        !_endpoint.Contains("openai.azure.com", StringComparison.OrdinalIgnoreCase);

        if (_isConfigured)
        {
            var credential = new AzureKeyCredential(_apiKey);
            var uri = new Uri(_endpoint);
            _indexClient = new SearchIndexClient(uri, credential);
            _searchClient = new SearchClient(uri, _indexName, credential);
            _logger.LogInformation("[AzureAiSearch] Initialized search client for endpoint {Endpoint} and index {IndexName}", _endpoint, _indexName);
        }
        else
        {
            throw new InvalidOperationException("Azure AI Search endpoint and API key are required. Local search is disabled.");
        }
    }

    public async Task EnsureIndexCreatedAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _indexClient == null || _indexEnsured)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexEnsured)
                return;

            _logger.LogInformation("[AzureAiSearch] Verifying search index '{IndexName}' exists...", _indexName);

            try
            {
                await _indexClient.GetIndexAsync(_indexName, cancellationToken);
                _indexEnsured = true;
                _logger.LogInformation("[AzureAiSearch] Search index '{IndexName}' is ready.", _indexName);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("[AzureAiSearch] Search index '{IndexName}' was not found. Creating it...", _indexName);
            }

            var fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(KnowledgeIndexDocument));

            var definition = new SearchIndex(_indexName, searchFields);

            var vectorSearch = new VectorSearch();
            var hnswConfig = new HnswAlgorithmConfiguration("knowledge-hnsw-algorithm")
            {
                Parameters = new HnswParameters
                {
                    Metric = VectorSearchAlgorithmMetric.Cosine
                }
            };
            vectorSearch.Algorithms.Add(hnswConfig);

            var vectorProfile = new VectorSearchProfile("knowledge-vector-profile", "knowledge-hnsw-algorithm");
            vectorSearch.Profiles.Add(vectorProfile);

            definition.VectorSearch = vectorSearch;

            await _indexClient.CreateIndexAsync(definition, cancellationToken: cancellationToken);
            _indexEnsured = true;
            _logger.LogInformation("[AzureAiSearch] Search index '{IndexName}' is verified and ready.", _indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureAiSearch] Failed to ensure index '{IndexName}'.", _indexName);
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task UpsertChunkAsync(KnowledgeDocumentDto chunk, CancellationToken cancellationToken = default)
    {
        await UpsertChunksBatchAsync(new[] { chunk }, cancellationToken);
    }

    public async Task UpsertChunksBatchAsync(IEnumerable<KnowledgeDocumentDto> chunks, CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        if (chunkList.Count == 0)
            return;

        if (!_isConfigured || _searchClient == null)
            throw new InvalidOperationException("Azure AI Search is not configured. Local search is disabled.");

        try
        {
            await EnsureIndexCreatedAsync(cancellationToken);

            _logger.LogInformation("[AzureAiSearch] Vectorizing and uploading {Count} chunks to index '{IndexName}'...", chunkList.Count, _indexName);

            var textsToEmbed = chunkList.Select(c => $"{c.DocumentName} - {c.Section}\n{c.Content}").ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(textsToEmbed, cancellationToken);

            var indexDocuments = new List<KnowledgeIndexDocument>();
            for (int i = 0; i < chunkList.Count; i++)
            {
                var c = chunkList[i];
                var embedding = (i < embeddings.Count) ? embeddings[i] : null;

                // Sanitize ID to ensure it is valid for Azure AI Search keys (alphanumeric, dash, underscore)
                string safeKey = SanitizeIndexKey(c.Id);

                indexDocuments.Add(new KnowledgeIndexDocument
                {
                    Id = safeKey,
                    DocumentName = c.DocumentName,
                    Version = c.Version,
                    Section = c.Section,
                    Page = c.Page,
                    Content = c.Content,
                    Approved = c.Approved,
                    Active = c.Active,
                    BlobPath = c.BlobPath,
                    LastModified = c.LastModified,
                    ContentVector = embedding?.ToArray()
                });
            }

            var batch = IndexDocumentsBatch.Upload(indexDocuments);
            var result = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
            _logger.LogInformation("[AzureAiSearch] Successfully indexed {Count} chunks into '{IndexName}'.", result.Value.Results.Count, _indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureAiSearch] Error indexing chunks into Azure AI Search.");
            throw;
        }
    }

    public async Task<int> GetIndexedCountAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _searchClient == null)
            throw new InvalidOperationException("Azure AI Search is not configured. Local search is disabled.");

        try
        {
            var countResponse = await _searchClient.GetDocumentCountAsync(cancellationToken);
            return (int)countResponse.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureAiSearch] Failed to get document count.");
            throw;
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _indexClient == null)
            throw new InvalidOperationException("Azure AI Search is not configured. Local search is disabled.");

        try
        {
            await _indexClient.DeleteIndexAsync(_indexName, cancellationToken);
            _indexEnsured = false;
            _logger.LogInformation("[AzureAiSearch] Index '{IndexName}' was deleted for reset.", _indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureAiSearch] Error clearing index '{IndexName}'.", _indexName);
            throw;
        }
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _searchClient == null)
            throw new InvalidOperationException("Azure AI Search is not configured. Local search is disabled.");

        try
        {
            await EnsureIndexCreatedAsync(cancellationToken);

            var searchOptions = new SearchOptions
            {
                Size = query.TopK > 0 ? query.TopK : 5
            };

            if (query.FilterApprovedOnly)
            {
                searchOptions.Filter = "approved eq true and active eq true";
            }

            // Generate query embedding for Vector / Hybrid Search
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query.QueryText, cancellationToken);
            if (queryEmbedding.HasValue)
            {
                var vectorQuery = new VectorizedQuery(queryEmbedding.Value)
                {
                    KNearestNeighborsCount = query.TopK > 0 ? query.TopK : 5
                };
                vectorQuery.Fields.Add("contentVector");

                searchOptions.VectorSearch = new VectorSearchOptions();
                searchOptions.VectorSearch.Queries.Add(vectorQuery);
            }

            // Execute Azure AI Search Hybrid Search (Text query + Vector query)
            _logger.LogInformation("[AzureAiSearch] Performing Hybrid Search for query: '{QueryText}'", query.QueryText);
            SearchResults<KnowledgeIndexDocument> response = await _searchClient.SearchAsync<KnowledgeIndexDocument>(
                query.QueryText,
                searchOptions,
                cancellationToken);

            var results = new List<SearchResultDto>();
            await foreach (SearchResult<KnowledgeIndexDocument> searchResult in response.GetResultsAsync())
            {
                var doc = searchResult.Document;
                double score = searchResult.Score ?? 1.0;

                results.Add(new SearchResultDto(
                    DocumentName: doc.DocumentName,
                    Version: doc.Version,
                    Section: doc.Section,
                    Page: doc.Page,
                    Content: doc.Content,
                    Score: score,
                    Approved: doc.Approved,
                    Active: doc.Active,
                    BlobPath: doc.BlobPath
                ));
            }

            _logger.LogInformation("[AzureAiSearch] Hybrid search returned {Count} matching chunks.", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureAiSearch] Exception during hybrid search.");
            throw;
        }
    }

    private static string SanitizeIndexKey(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return Guid.NewGuid().ToString("N");

        var sb = new System.Text.StringBuilder();
        foreach (char c in rawKey)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }
}
