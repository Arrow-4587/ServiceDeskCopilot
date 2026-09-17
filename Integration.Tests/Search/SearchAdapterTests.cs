using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Infrastructure.Adapters.Search;
using ServiceDesk.Infrastructure.Adapters.Storage;
using ServiceDesk.Infrastructure.Services;
using NUnit.Framework;

namespace Integration.Tests.Search;

[TestFixture]
public class SearchAdapterTests
{
    [Test]
    public async Task SearchKnowledgeAsync_ReturnsRelevantRankedResults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var sourceStore = new LocalFileKnowledgeSourceStore(configuration);
        var searchAdapter = new InMemoryKnowledgeSearchAdapter();
        var logger = NullLogger<KnowledgeIngestionService>.Instance;

        var ingestionService = new KnowledgeIngestionService(sourceStore, searchAdapter, logger);
        await ingestionService.IngestApprovedKnowledgeDocumentsAsync();

        // Act
        var query = new SearchQueryDto("VPN Cisco AnyConnect MFA", TopK: 5, FilterApprovedOnly: true);
        var results = await searchAdapter.SearchKnowledgeAsync(query);

        // Assert
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.Count, Is.LessThanOrEqualTo(5));
        Assert.That(results.All(r => r.Approved && r.Active), Is.True);
        Assert.That(results.All(r => r.Score > 0), Is.True);
        Assert.That(results.Any(r => r.DocumentName.Contains("VPN", StringComparison.OrdinalIgnoreCase) || r.Content.Contains("VPN", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public async Task SearchKnowledgeAsync_FiltersUnapprovedOrInactiveDocumentsWhenRequested()
    {
        // Arrange
        var adapter = new InMemoryKnowledgeSearchAdapter();
        
        var approvedDoc = new KnowledgeDocumentDto(
            Id: "chunk-1",
            DocumentName: "Approved Policy",
            Version: "1.0",
            Section: "General",
            Page: 1,
            Content: "VPN connection instructions and settings.",
            Approved: true,
            Active: true,
            BlobPath: "data/knowledge/approved.md",
            LastModified: DateTime.UtcNow
        );

        var unapprovedDoc = new KnowledgeDocumentDto(
            Id: "chunk-2",
            DocumentName: "Draft Policy",
            Version: "0.1",
            Section: "General",
            Page: 1,
            Content: "VPN connection draft guidance.",
            Approved: false,
            Active: true,
            BlobPath: "data/knowledge/draft.md",
            LastModified: DateTime.UtcNow
        );

        await adapter.UpsertChunkAsync(approvedDoc);
        await adapter.UpsertChunkAsync(unapprovedDoc);

        // Act
        var resultsFiltered = await adapter.SearchKnowledgeAsync(new SearchQueryDto("VPN", FilterApprovedOnly: true));
        var resultsUnfiltered = await adapter.SearchKnowledgeAsync(new SearchQueryDto("VPN", FilterApprovedOnly: false));

        // Assert
        Assert.That(resultsFiltered.Count, Is.EqualTo(1));
        Assert.That(resultsFiltered[0].DocumentName, Is.EqualTo("Approved Policy"));

        Assert.That(resultsUnfiltered.Count, Is.EqualTo(2));
    }
}
