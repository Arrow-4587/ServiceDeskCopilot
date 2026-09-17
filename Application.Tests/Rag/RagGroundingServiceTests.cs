using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Services;

namespace Application.Tests.Rag;

[TestFixture]
public class RagGroundingServiceTests
{
    private class FakeRetriever : IKnowledgeRetriever
    {
        public List<SearchResultDto> ResultsToReturn { get; set; } = new();

        public Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SearchResultDto>>(ResultsToReturn);
        }
    }

    private class FakeChatModel : IChatModel
    {
        public string ModelResponseText { get; set; } = "To connect to corporate VPN, open Cisco AnyConnect and select vpn.company.com.";

        public Task<ChatResponseDto> GenerateCompletionAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var response = new ChatResponseDto(
                ConversationId: request.ConversationId ?? Guid.NewGuid(),
                Answer: ModelResponseText,
                Citations: Array.Empty<CitationDto>()
            );
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(ChatRequestDto request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return ModelResponseText;
            await Task.CompletedTask;
        }
    }

    [Test]
    public async Task GenerateGroundedAnswerAsync_ReturnsGroundedAnswerAndCitations_WhenApprovedMatchingKnowledgeExists()
    {
        // Arrange
        var retriever = new FakeRetriever
        {
            ResultsToReturn = new List<SearchResultDto>
            {
                new SearchResultDto(
                    DocumentName: "VPN Policy",
                    Version: "1.0",
                    Section: "Setup",
                    Page: 1,
                    Content: "Open Cisco AnyConnect client and connect to vpn.company.com.",
                    Score: 3.5,
                    Approved: true,
                    Active: true,
                    BlobPath: "data/knowledge/vpn_policy.md"
                )
            }
        };

        var chatModel = new FakeChatModel();
        var logger = NullLogger<RagGroundingService>.Instance;
        var service = new RagGroundingService(retriever, chatModel, logger);

        // Act
        var result = await service.GenerateGroundedAnswerAsync("How do I connect to VPN?");

        // Assert
        Assert.That(result.Grounded, Is.True);
        Assert.That(result.TriggeredFallback, Is.False);
        Assert.That(result.Answer, Is.EqualTo(chatModel.ModelResponseText));
        Assert.That(result.Citations.Count, Is.EqualTo(1));
        Assert.That(result.Citations[0].DocumentName, Is.EqualTo("VPN Policy"));
        Assert.That(result.Citations[0].BlobPath, Is.EqualTo("data/knowledge/vpn_policy.md"));
    }

    [Test]
    public async Task GenerateGroundedAnswerAsync_TriggersFallbackProtocol_WhenZeroOrLowScoreResultsReturned()
    {
        // Arrange
        var retriever = new FakeRetriever
        {
            ResultsToReturn = new List<SearchResultDto>() // No matches
        };

        var chatModel = new FakeChatModel();
        var logger = NullLogger<RagGroundingService>.Instance;
        var service = new RagGroundingService(retriever, chatModel, logger);

        // Act
        var result = await service.GenerateGroundedAnswerAsync("What is the secret formula?");

        // Assert
        Assert.That(result.Grounded, Is.False);
        Assert.That(result.TriggeredFallback, Is.True);
        Assert.That(result.Answer, Is.EqualTo(RagGroundingService.FallbackMessage));
        Assert.That(result.Citations, Is.Empty);
    }

    [Test]
    public async Task GenerateGroundedAnswerAsync_EnforcesGroundingInvariant_FiltersUnapprovedOrInactiveDocs()
    {
        // Arrange
        var retriever = new FakeRetriever
        {
            ResultsToReturn = new List<SearchResultDto>
            {
                new SearchResultDto(
                    DocumentName: "Unapproved Policy",
                    Version: "0.1",
                    Section: "Draft",
                    Page: 1,
                    Content: "Draft VPN instructions.",
                    Score: 4.0,
                    Approved: false, // Unapproved!
                    Active: true,
                    BlobPath: "data/knowledge/draft.md"
                )
            }
        };

        var chatModel = new FakeChatModel();
        var logger = NullLogger<RagGroundingService>.Instance;
        var service = new RagGroundingService(retriever, chatModel, logger);

        // Act
        var result = await service.GenerateGroundedAnswerAsync("How do I connect to VPN?");

        // Assert - Fallback must trigger due to Grounding Invariant
        Assert.That(result.Grounded, Is.False);
        Assert.That(result.TriggeredFallback, Is.True);
        Assert.That(result.Answer, Is.EqualTo(RagGroundingService.FallbackMessage));
    }
}
