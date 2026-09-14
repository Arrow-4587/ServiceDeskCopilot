using System.Runtime.CompilerServices;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class MockAiChatModelAdapter : IChatModel
{
    private readonly SecretRedactionService _redactionService;

    public MockAiChatModelAdapter(SecretRedactionService redactionService)
    {
        _redactionService = redactionService ?? throw new ArgumentNullException(nameof(redactionService));
    }

    public Task<ChatResponseDto> GenerateCompletionAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = _redactionService.RedactSecrets(request.UserMessage);

        var citations = new List<CitationDto>
        {
            new CitationDto("Global IT Connectivity Policy", "Section 3.1", 4, "Employees must connect to Corporate Cisco AnyConnect VPN prior to accessing internal portals.", "data/knowledge/vpn_policy.md")
        };

        bool suggestsDraft = sanitizedMessage.Contains("vpn", StringComparison.OrdinalIgnoreCase) || 
                             sanitizedMessage.Contains("printer", StringComparison.OrdinalIgnoreCase) ||
                             sanitizedMessage.Contains("issue", StringComparison.OrdinalIgnoreCase) ||
                             sanitizedMessage.Contains("error", StringComparison.OrdinalIgnoreCase);

        var response = new ChatResponseDto(
            ConversationId: request.ConversationId,
            Answer: $"[Mock AI Service Desk] Based on corporate IT policies, please ensure your device is registered on Azure AD and your network connection is active. Query received: '{sanitizedMessage}'.",
            Citations: citations,
            RequiresClarification: false,
            SuggestedIncidentDraft: suggestsDraft,
            SuggestedTitle: suggestsDraft ? "IT Connectivity / System Issue" : null,
            SuggestedDescription: suggestsDraft ? sanitizedMessage : null
        );

        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = _redactionService.RedactSecrets(request.UserMessage);
        var responseText = $"[Mock AI Service Desk] Analyzing issue: '{sanitizedMessage}'. Step 1: Verify network configuration. Step 2: Check VPN status. Step 3: If unresolved, approve draft incident submission.";

        var words = responseText.Split(' ');
        foreach (var word in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return word + " ";
            await Task.Delay(10, cancellationToken);
        }
    }
}
