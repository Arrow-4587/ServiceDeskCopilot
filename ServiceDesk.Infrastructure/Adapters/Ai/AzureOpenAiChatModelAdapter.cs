using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class AzureOpenAiChatModelAdapter : IChatModel
{
    private readonly SecretRedactionService _redactionService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureOpenAiChatModelAdapter> _logger;
    private readonly string _endpoint;
    private readonly string _deploymentName;
    private readonly string _apiKey;

    public AzureOpenAiChatModelAdapter(
        IConfiguration configuration,
        SecretRedactionService redactionService,
        HttpClient httpClient,
        ILogger<AzureOpenAiChatModelAdapter> logger)
    {
        _redactionService = redactionService ?? throw new ArgumentNullException(nameof(redactionService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _endpoint = configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/') ?? string.Empty;
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        _apiKey = configuration["AzureOpenAI:ApiKey"] ?? string.Empty;
    }

    public async Task<ChatResponseDto> GenerateCompletionAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = _redactionService.RedactSecrets(request.UserMessage);
        var sanitizedRequest = request with { UserMessage = sanitizedMessage };

        if (string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("[AzureOpenAI] Cloud credentials missing.");
            return new ChatResponseDto(
                ConversationId: request.ConversationId ?? Guid.NewGuid(),
                Answer: "Error: Cloud AI credentials missing.",
                Citations: new List<CitationDto>(),
                RequiresClarification: false,
                SuggestedIncidentDraft: true,
                SuggestedTitle: "Cloud AI credentials missing",
                SuggestedDescription: "Please configure Azure OpenAI.",
                SuggestedDraftId: null);
        }

        var deploymentsToTry = new List<string> { _deploymentName };

        foreach (var deployment in deploymentsToTry)
        {
            try
            {
                var url = $"{_endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-01";
                var payload = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are an Enterprise IT Service Desk Copilot. Provide concise, grounded diagnostic answers to employee inquiries." },
                        new { role = "user", content = sanitizedMessage }
                    },
                    temperature = 1.0f
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                req.Headers.Add("api-key", _apiKey);

                var res = await _httpClient.SendAsync(req, cancellationToken);
                if (!res.IsSuccessStatusCode)
                {
                    var err = await res.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("[AzureOpenAI] Deployment '{Deployment}' returned status {StatusCode}: {Error}.", deployment, res.StatusCode, err);
                    continue;
                }

                var json = await res.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                
                var answerText = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                var redactedAnswer = _redactionService.RedactSecrets(answerText);

                return new ChatResponseDto(
                    ConversationId: request.ConversationId ?? Guid.NewGuid(),
                    Answer: redactedAnswer,
                    Citations: new List<CitationDto>
                    {
                        new CitationDto("Azure OpenAI Service", deployment, 1, $"Grounded response generated live by Azure OpenAI model deployment '{deployment}'.", _endpoint)
                    },
                    RequiresClarification: false,
                    SuggestedIncidentDraft: false,
                    SuggestedTitle: null,
                    SuggestedDescription: null,
                    SuggestedDraftId: null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AzureOpenAI] Error invoking deployment '{Deployment}'.", deployment);
            }
        }

        _logger.LogError("[AzureOpenAI] All configured Azure OpenAI deployments failed.");
        return new ChatResponseDto(
            ConversationId: request.ConversationId ?? Guid.NewGuid(),
            Answer: "Error: All configured Azure OpenAI deployments failed.",
            Citations: new List<CitationDto>(),
            RequiresClarification: false,
            SuggestedIncidentDraft: true,
            SuggestedTitle: "Azure OpenAI failure",
            SuggestedDescription: "Failed to connect to Azure OpenAI deployments.",
            SuggestedDraftId: null);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = _redactionService.RedactSecrets(request.UserMessage);
        var sanitizedRequest = request with { UserMessage = sanitizedMessage };

        var response = await GenerateCompletionAsync(sanitizedRequest, cancellationToken);
        yield return response.Answer;
    }
}
