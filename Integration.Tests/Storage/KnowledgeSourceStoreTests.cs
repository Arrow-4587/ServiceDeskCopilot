using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using ServiceDesk.Infrastructure.Adapters.Storage;

namespace Integration.Tests.Storage;

[TestFixture]
public class KnowledgeSourceStoreTests
{
    private LocalFileKnowledgeSourceStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new ConfigurationBuilder().Build();
        _store = new LocalFileKnowledgeSourceStore(config);
    }

    [Test]
    public async Task GetApprovedSourceDocumentsAsync_ShouldReturnAll10SyntheticKnowledgeDocuments()
    {
        var docs = await _store.GetApprovedSourceDocumentsAsync();

        Assert.That(docs, Is.Not.Null);
        Assert.That(docs.Count, Is.GreaterThanOrEqualTo(10), "Should find at least 10 synthetic knowledge documents in data/knowledge.");

        foreach (var doc in docs)
        {
            Assert.That(doc.DocumentName, Is.Not.Empty);
            Assert.That(doc.Approved, Is.True);
            Assert.That(doc.Active, Is.True);
            Assert.That(doc.BlobPath, Does.StartWith("data/knowledge/"));
            Assert.That(doc.Content, Is.Not.Empty);
        }
    }

    [Test]
    public async Task GetDocumentStreamAsync_ShouldReturnOpenStream_WhenFileExists()
    {
        var docs = await _store.GetApprovedSourceDocumentsAsync();
        Assert.That(docs.Count, Is.GreaterThan(0));

        var firstDoc = docs.First();
        using var stream = await _store.GetDocumentStreamAsync(firstDoc.BlobPath);

        Assert.That(stream, Is.Not.Null);
        Assert.That(stream!.CanRead, Is.True);
    }
}
