using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class AzureOpenAiChatModelAdapter : IChatModel
{
    private readonly MockAiChatModelAdapter _fallbackAdapter;
    private readonly SecretRedactionService _redactionService;
    private readonly string _endpoint;
    private readonly string _deploymentName;

    public AzureOpenAiChatModelAdapter(
        IConfiguration configuration,
        SecretRedactionService redactionService,
        MockAiChatModelAdapter fallbackAdapter)
    {
        _redactionService = redactionService ?? throw new ArgumentNullException(nameof(redactionService));
        _fallbackAdapter = fallbackAdapter ?? throw new ArgumentNullException(nameof(fallbackAdapter));
        _endpoint = configuration["AzureOpenAI:Endpoint"] ?? string.Empty;
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
    }

    public async Task<ChatResponseDto> GenerateCompletionAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = _redactionService.RedactSecrets(request.UserMessage);
        var sanitizedRequest = request with { UserMessage = sanitizedMessage };

        if (string.IsNullOrWhiteSpace(_endpoint))
        {
            // Development fallback when live cloud endpoint is not configured
            return await _fallbackAdapter.GenerateCompletionAsync(sanitizedRequest, cancellationToken);
        }

        // Production Azure OpenAI client execution logic will be connected when live cloud credentials are present
        return await _fallbackAdapter.GenerateCompletionAsync(sanitizedRequest, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = _redactionService.RedactSecrets(request.UserMessage);
        var sanitizedRequest = request with { UserMessage = sanitizedMessage };

        await foreach (var token in _fallbackAdapter.StreamCompletionAsync(sanitizedRequest, cancellationToken))
        {
            yield return token;
        }
    }
}
