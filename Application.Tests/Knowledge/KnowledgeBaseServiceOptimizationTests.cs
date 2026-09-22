using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Services.Knowledge;

namespace Application.Tests.Knowledge;

[TestFixture]
public class KnowledgeBaseServiceOptimizationTests
{
    [Test]
    public async Task GetApprovedDocumentsAsync_UsesMemoryCacheToPreventRedundantSourceFetches()
    {
        var fakeStore = new CountingKnowledgeSourceStore
        {
            Documents = new List<KnowledgeDocumentDto>
            {
                new("doc1", "VPN Policy", "1.0", "Network", 1, "VPN Content", Approved: true, Active: true, "blob/vpn.md", DateTime.UtcNow)
            }
        };

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KnowledgeBaseService(fakeStore, NullLogger<KnowledgeBaseService>.Instance, cache);

        // First call - should hit source store
        var docs1 = await service.GetApprovedDocumentsAsync();
        Assert.That(fakeStore.FetchCount, Is.EqualTo(1));
        Assert.That(docs1.Count, Is.EqualTo(1));

        // Second call - should serve from IMemoryCache without calling source store again
        var docs2 = await service.GetApprovedDocumentsAsync();
        Assert.That(fakeStore.FetchCount, Is.EqualTo(1), "Memory cache must prevent secondary source store fetches.");
        Assert.That(docs2.Count, Is.EqualTo(1));
    }

    private class CountingKnowledgeSourceStore : IKnowledgeSourceStore
    {
        public List<KnowledgeDocumentDto> Documents { get; set; } = new();
        public int FetchCount { get; private set; }

        public Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default)
        {
            FetchCount++;
            return Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(Documents);
        }

        public Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(null);
        }

        public Task<KnowledgeDocumentDto> UploadDocumentAsync(string fileName, Stream contentStream, string contentType = "text/markdown", CancellationToken cancellationToken = default)
        {
            var doc = new KnowledgeDocumentDto(fileName, fileName, "1.0", "General", 1, string.Empty, true, true, $"blob/{fileName}", DateTime.UtcNow);
            Documents.Add(doc);
            return Task.FromResult(doc);
        }
    }
}
