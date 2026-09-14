using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Infrastructure.Adapters.Search;
using ServiceDesk.Infrastructure.Adapters.Storage;
using ServiceDesk.Infrastructure.Services;
using NUnit.Framework;

namespace Integration.Tests.Search;

[TestFixture]
public class KnowledgeIngestionServiceTests
{
    [Test]
    public async Task IngestApprovedKnowledgeDocumentsAsync_IngestsApprovedFilesAndIsIdempotent()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var sourceStore = new LocalFileKnowledgeSourceStore(configuration);
        var indexStore = new InMemoryKnowledgeSearchAdapter();
        var logger = NullLogger<KnowledgeIngestionService>.Instance;

        var service = new KnowledgeIngestionService(sourceStore, indexStore, logger);

        // Act 1 - First Ingestion
        int firstRunCount = await service.IngestApprovedKnowledgeDocumentsAsync();
        int indexedCountAfterFirstRun = await indexStore.GetIndexedCountAsync();

        // Assert 1
        Assert.That(firstRunCount, Is.GreaterThan(0), "Ingestion should produce at least 1 chunk from sample knowledge files.");
        Assert.That(indexedCountAfterFirstRun, Is.EqualTo(firstRunCount));

        // Act 2 - Second Ingestion (Idempotency Check)
        int secondRunCount = await service.IngestApprovedKnowledgeDocumentsAsync();
        int indexedCountAfterSecondRun = await indexStore.GetIndexedCountAsync();

        // Assert 2 - Idempotency
        Assert.That(secondRunCount, Is.EqualTo(firstRunCount));
        Assert.That(indexedCountAfterSecondRun, Is.EqualTo(indexedCountAfterFirstRun));
    }

    [Test]
    public void ChunkDocument_FiltersAndPreservesMetadataAndDeterministicIds()
    {
        // Arrange
        var doc = new KnowledgeDocumentDto(
            Id: "doc-01",
            DocumentName: "VPN Access Policy",
            Version: "2.1",
            Section: "Security",
            Page: 1,
            Content: "# Header Section 1\nThis is paragraph one.\n\n## Sub Header 2\nThis is sub section two.",
            Approved: true,
            Active: true,
            BlobPath: "data/knowledge/vpn_access.md",
            LastModified: DateTime.UtcNow
        );

        // Act
        var chunks = KnowledgeIngestionService.ChunkDocument(doc);

        // Assert
        Assert.That(chunks.Count, Is.EqualTo(2));
        Assert.That(chunks[0].Section, Is.EqualTo("Header Section 1"));
        Assert.That(chunks[1].Section, Is.EqualTo("Sub Header 2"));
        Assert.That(chunks.All(c => c.Approved && c.Active), Is.True);
        Assert.That(chunks.All(c => c.BlobPath == "data/knowledge/vpn_access.md"), Is.True);

        // Deterministic ID check
        string expectedId0 = KnowledgeIngestionService.GenerateDeterministicChunkId("VPN Access Policy", "Header Section 1", 1, 1);
        Assert.That(chunks[0].Id, Is.EqualTo(expectedId0));
    }
}
