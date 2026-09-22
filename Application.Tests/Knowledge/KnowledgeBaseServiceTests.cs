using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Services.Knowledge;

namespace Application.Tests.Knowledge;

[TestFixture]
public class KnowledgeBaseServiceTests
{
    private FakeKnowledgeSourceStore _fakeStore = null!;
    private KnowledgeBaseService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeStore = new FakeKnowledgeSourceStore();
        _service = new KnowledgeBaseService(_fakeStore, NullLogger<KnowledgeBaseService>.Instance);
    }

    [Test]
    public async Task GetApprovedDocumentsAsync_ReturnsOnlyApprovedAndActiveDocuments()
    {
        _fakeStore.Documents = new List<KnowledgeDocumentDto>
        {
            new("doc1", "VPN Policy", "1.0", "Network & Connectivity", 1, "Content 1", Approved: true, Active: true, "blob/vpn.md", DateTime.UtcNow),
            new("doc2", "Draft Policy", "0.1", "Drafts", 1, "Content 2", Approved: false, Active: true, "blob/draft.md", DateTime.UtcNow),
            new("doc3", "Legacy Policy", "1.0", "Security", 1, "Content 3", Approved: true, Active: false, "blob/legacy.md", DateTime.UtcNow),
            new("doc4", "MFA Setup", "1.1", "Security & Access", 1, "Content 4", Approved: true, Active: true, "blob/mfa.md", DateTime.UtcNow),
        };

        var result = await _service.GetApprovedDocumentsAsync();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(d => d.Id == "doc1"), Is.True);
        Assert.That(result.Any(d => d.Id == "doc4"), Is.True);
        Assert.That(result.Any(d => d.Id == "doc2"), Is.False, "Unapproved documents must not be exposed.");
        Assert.That(result.Any(d => d.Id == "doc3"), Is.False, "Inactive documents must not be exposed.");
    }

    [Test]
    public async Task GetDocumentDetailAsync_RendersMarkdownToSafeHtmlWithoutUnsafeScriptExecution()
    {
        var markdownWithScript = "# Title\n\nSome paragraph\n\n<script>alert('xss')</script>";

        _fakeStore.Documents = new List<KnowledgeDocumentDto>
        {
            new("sec1", "Security Guide", "1.0", "Security", 1, markdownWithScript, Approved: true, Active: true, "blob/sec1.md", DateTime.UtcNow)
        };

        var detail = await _service.GetDocumentDetailAsync("sec1");

        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.Document.DocumentName, Is.EqualTo("Security Guide"));
        Assert.That(detail.RenderedHtml, Contains.Substring("Title"));
        Assert.That(detail.RenderedHtml, Contains.Substring("Some paragraph"));
        Assert.That(detail.RenderedHtml, Does.Not.Contain("<script>"), "Inline script tags must be sanitized/disabled.");
        Assert.That(detail.RenderedHtml, Contains.Substring("&lt;script&gt;"), "Raw HTML tags must be HTML-encoded.");
    }

    [Test]
    public async Task GetDocumentDetailAsync_ReturnsNullForMissingOrUnapprovedDocument()
    {
        _fakeStore.Documents = new List<KnowledgeDocumentDto>
        {
            new("unapproved1", "Secret Policy", "1.0", "Confidential", 1, "Secret content", Approved: false, Active: true, "blob/secret.md", DateTime.UtcNow)
        };

        var detailMissing = await _service.GetDocumentDetailAsync("nonexistent");
        var detailUnapproved = await _service.GetDocumentDetailAsync("unapproved1");

        Assert.That(detailMissing, Is.Null);
        Assert.That(detailUnapproved, Is.Null, "Unapproved documents must return null on detail fetch.");
    }

    [Test]
    public void GetApprovedDocumentsAsync_PropagatesExceptionWhenAzureSourceFails()
    {
        _fakeStore.ShouldThrowException = true;

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.GetApprovedDocumentsAsync();
        }, "Should propagate Azure Blob Storage store exceptions without falling back to local files.");
    }

    private class FakeKnowledgeSourceStore : IKnowledgeSourceStore
    {
        public List<KnowledgeDocumentDto> Documents { get; set; } = new();
        public bool ShouldThrowException { get; set; }

        public Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
            {
                throw new InvalidOperationException("Azure Blob Storage container is unavailable.");
            }
            return Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(Documents);
        }

        public Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
            {
                throw new InvalidOperationException("Azure Blob Storage container is unavailable.");
            }
            return Task.FromResult<Stream?>(null);
        }

        public Task<KnowledgeDocumentDto> UploadDocumentAsync(string fileName, Stream contentStream, string contentType = "text/markdown", CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
            {
                throw new InvalidOperationException("Azure Blob Storage container is unavailable.");
            }
            var doc = new KnowledgeDocumentDto(fileName, fileName, "1.0", "General", 1, string.Empty, true, true, $"blob/{fileName}", DateTime.UtcNow);
            Documents.Add(doc);
            return Task.FromResult(doc);
        }
    }

    [Test]
    public async Task KnowledgeBaseService_InvalidateCache_ClearsCachedDocuments()
    {
        using var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var cachingService = new KnowledgeBaseService(_fakeStore, NullLogger<KnowledgeBaseService>.Instance, memoryCache);

        _fakeStore.Documents = new List<KnowledgeDocumentDto>
        {
            new("doc1", "Doc 1", "1.0", "Sec", 1, "Content", true, true, "blob/doc1.md", DateTime.UtcNow)
        };

        var initialDocs = await cachingService.GetApprovedDocumentsAsync();
        Assert.That(initialDocs.Count, Is.EqualTo(1));

        _fakeStore.Documents.Add(new("doc2", "Doc 2", "1.0", "Sec", 1, "Content 2", true, true, "blob/doc2.md", DateTime.UtcNow));

        var cachedDocs = await cachingService.GetApprovedDocumentsAsync();
        Assert.That(cachedDocs.Count, Is.EqualTo(1));

        cachingService.InvalidateCache();

        var refreshedDocs = await cachingService.GetApprovedDocumentsAsync();
        Assert.That(refreshedDocs.Count, Is.EqualTo(2));
    }
}
