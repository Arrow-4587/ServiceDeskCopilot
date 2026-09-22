using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.Services.Knowledge;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IKnowledgeSourceStore _sourceStore;
    private readonly IMemoryCache? _cache;
    private readonly ILogger<KnowledgeBaseService> _logger;
    private readonly MarkdownPipeline _markdownPipeline;

    private const string ApprovedDocsCacheKey = "ServiceDesk_ApprovedKnowledgeDocs";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public KnowledgeBaseService(
        IKnowledgeSourceStore sourceStore,
        ILogger<KnowledgeBaseService> logger,
        IMemoryCache? cache = null)
    {
        _sourceStore = sourceStore ?? throw new ArgumentNullException(nameof(sourceStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;

        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();
    }

    public async Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache != null && _cache.TryGetValue<IReadOnlyList<KnowledgeDocumentDto>>(ApprovedDocsCacheKey, out var cachedDocs) && cachedDocs != null)
        {
            _logger.LogDebug("[KnowledgeBaseService] Returning {Count} approved knowledge documents from memory cache.", cachedDocs.Count);
            return cachedDocs;
        }

        _logger.LogInformation("[KnowledgeBaseService] Fetching approved and active knowledge documents from configured source...");

        var allDocs = await _sourceStore.GetApprovedSourceDocumentsAsync(cancellationToken);

        var approvedDocs = allDocs
            .Where(d => d.Approved && d.Active)
            .OrderBy(d => d.Section)
            .ThenBy(d => d.DocumentName)
            .ToList();

        _logger.LogInformation("[KnowledgeBaseService] Found {Count} approved and active documents out of {Total} total retrieved documents.", approvedDocs.Count, allDocs.Count);

        if (_cache != null)
        {
            _cache.Set(ApprovedDocsCacheKey, (IReadOnlyList<KnowledgeDocumentDto>)approvedDocs, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });
        }

        return approvedDocs;
    }

    public async Task<KnowledgeDocumentDetailDto?> GetDocumentDetailAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        string detailCacheKey = $"ServiceDesk_KnowledgeDocDetail_{id.ToLowerInvariant()}";
        if (_cache != null && _cache.TryGetValue<KnowledgeDocumentDetailDto>(detailCacheKey, out var cachedDetail) && cachedDetail != null)
        {
            return cachedDetail;
        }

        var approvedDocs = await GetApprovedDocumentsAsync(cancellationToken);
        var doc = approvedDocs.FirstOrDefault(d => 
            d.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
            d.DocumentName.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (doc == null)
        {
            _logger.LogWarning("[KnowledgeBaseService] Document with ID '{Id}' was not found or is not approved/active.", id);
            return null;
        }

        var renderedHtml = Markdown.ToHtml(doc.Content ?? string.Empty, _markdownPipeline);

        var detail = new KnowledgeDocumentDetailDto(doc, renderedHtml);

        if (_cache != null)
        {
            _cache.Set(detailCacheKey, detail, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });
        }

        return detail;
    }

    public void InvalidateCache()
    {
        if (_cache != null)
        {
            _logger.LogInformation("[KnowledgeBaseService] Evicting approved documents memory cache after document upload/ingestion.");
            _cache.Remove(ApprovedDocsCacheKey);
        }
    }
}
