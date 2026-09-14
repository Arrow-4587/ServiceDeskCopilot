using NUnit.Framework;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Infrastructure.Adapters.Ai;
using ServiceDesk.Infrastructure.Services;

namespace Integration.Tests.Ai;

[TestFixture]
public class ChatModelAdapterTests
{
    private MockAiChatModelAdapter _adapter = null!;

    [SetUp]
    public void SetUp()
    {
        var redactionService = new SecretRedactionService();
        _adapter = new MockAiChatModelAdapter(redactionService);
    }

    [Test]
    public async Task GenerateCompletionAsync_ShouldReturnGroundedResponse_AndSuggestDraftForIssue()
    {
        var request = new ChatRequestDto(Guid.NewGuid(), "My VPN is failing with error 403.");

        var response = await _adapter.GenerateCompletionAsync(request);

        Assert.That(response, Is.Not.Null);
        Assert.That(response.Answer, Does.Contain("VPN"));
        Assert.That(response.Citations.Count, Is.GreaterThan(0));
        Assert.That(response.SuggestedIncidentDraft, Is.True);
    }

    [Test]
    public async Task StreamCompletionAsync_ShouldYieldSequenceOfTokens()
    {
        var request = new ChatRequestDto(Guid.NewGuid(), "Help me troubleshoot my laptop.");
        var tokens = new List<string>();

        await foreach (var token in _adapter.StreamCompletionAsync(request))
        {
            tokens.Add(token);
        }

        Assert.That(tokens.Count, Is.GreaterThan(0));
        var fullText = string.Join("", tokens);
        Assert.That(fullText, Does.Contain("laptop"));
    }

    [Test]
    public async Task GenerateCompletionAsync_ShouldRedactSecretFromPrompt_BeforeReturningAnswer()
    {
        var request = new ChatRequestDto(Guid.NewGuid(), "My secret key is api_key=sk-123456789012345678901234 please fix.");

        var response = await _adapter.GenerateCompletionAsync(request);

        Assert.That(response.Answer, Does.Not.Contain("sk-123456789012345678901234"));
        Assert.That(response.Answer, Does.Contain("[REDACTED_SECRET]"));
    }
}
