using NUnit.Framework;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.UseCases;

namespace Application.Tests;

[TestFixture]
public class SearchKnowledgeUseCaseTests
{
    private class MockKnowledgeRetriever : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
        {
            var results = new List<SearchResultDto>
            {
                new("VPN Policy", "1.0", "Sec 1", 1, "Approved active doc", 0.95, Approved: true, Active: true, BlobPath: "data/knowledge/vpn.md"),
                new("Draft Policy", "0.9", "Sec 2", 2, "Unapproved doc", 0.85, Approved: false, Active: true, BlobPath: "data/knowledge/draft.md"),
                new("Retired Policy", "1.0", "Sec 3", 3, "Approved inactive doc", 0.80, Approved: true, Active: false, BlobPath: "data/knowledge/retired.md")
            };

            return Task.FromResult<IReadOnlyList<SearchResultDto>>(results);
        }
    }

    [Test]
    public void ExecuteAsync_ShouldThrowValidationException_WhenQueryIsEmpty()
    {
        var retriever = new MockKnowledgeRetriever();
        var useCase = new SearchKnowledgeUseCase(retriever);

        Assert.ThrowsAsync<ValidationException>(async () => await useCase.ExecuteAsync(new SearchQueryDto("")));
    }

    [Test]
    public async Task ExecuteAsync_ShouldFilterOutUnapprovedOrInactiveDocuments()
    {
        var retriever = new MockKnowledgeRetriever();
        var useCase = new SearchKnowledgeUseCase(retriever);

        var results = await useCase.ExecuteAsync(new SearchQueryDto("VPN"));

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].DocumentName, Is.EqualTo("VPN Policy"));
        Assert.That(results[0].Approved, Is.True);
        Assert.That(results[0].Active, Is.True);
    }
}
