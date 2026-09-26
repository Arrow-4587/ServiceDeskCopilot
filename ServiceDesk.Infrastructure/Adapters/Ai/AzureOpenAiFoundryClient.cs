#pragma warning disable OPENAI001
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Assistants;
using OpenAI.Chat;

namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class AzureOpenAiFoundryClient : IFoundryAgentClient
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAiFoundryClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, (FoundryAgentDefinition Definition, DateTime ExpiresAt)> _agentCache = new(StringComparer.OrdinalIgnoreCase);

    public record FoundryAgentDefinition(
        string Id,
        string Name,
        string Instructions,
        string Model,
        string Kind);

    public AzureOpenAiFoundryClient(
        AzureOpenAIClient openAiClient,
        IConfiguration configuration,
        ILogger<AzureOpenAiFoundryClient> logger,
        HttpClient? httpClient = null)
    {
        _openAiClient = openAiClient ?? throw new ArgumentNullException(nameof(openAiClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<string> ExecuteAgentAsync(
        string agentNameOrId,
        string userPrompt,
        string? additionalInstructions = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentNameOrId))
            throw new ArgumentException("Agent name or ID cannot be empty.", nameof(agentNameOrId));

        if (string.IsNullOrWhiteSpace(userPrompt))
            throw new ArgumentException("User prompt cannot be empty.", nameof(userPrompt));

        var agentDef = await GetAgentDefinitionAsync(agentNameOrId, cancellationToken);

        var systemInstructions = agentDef.Instructions;
        if (!string.IsNullOrWhiteSpace(additionalInstructions))
        {
            systemInstructions = $"{systemInstructions}\n\n[APPLICATION CONTEXT & INSTRUCTIONS]:\n{additionalInstructions}";
        }

        string modelDeployment = agentDef.Model;
        if (string.IsNullOrWhiteSpace(modelDeployment))
        {
            modelDeployment = _configuration["AzureOpenAI:DeploymentName"]?.Trim() ?? "gpt-5-mini";
        }

        var chatClient = _openAiClient.GetChatClient(modelDeployment);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(systemInstructions),
            ChatMessage.CreateUserMessage(userPrompt)
        };

        _logger.LogInformation("[AzureAiFoundryClient] Executing agent '{Agent}' via deployment '{Deployment}'",
            agentNameOrId, modelDeployment);

        var stopwatch = Stopwatch.StartNew();
        int maxRetries = 3;
        int delayMs = 500;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var chatResult = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
                stopwatch.Stop();

                if (chatResult?.Value?.Content != null && chatResult.Value.Content.Count > 0)
                {
                    var text = chatResult.Value.Content[0].Text?.Trim() ?? string.Empty;
                    _logger.LogInformation("[AzureAiFoundryClient] Agent '{Agent}' completed in {Elapsed:F1}s ({Chars} chars)",
                        agentNameOrId, stopwatch.Elapsed.TotalSeconds, text.Length);
                    return text;
                }

                return string.Empty;
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                _logger.LogWarning("[AzureAiFoundryClient] Rate limit (429) for agent '{Agent}' on attempt {Attempt}/{MaxRetries}",
                    agentNameOrId, attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    throw new FoundryRateLimitException($"Azure AI Foundry rate limit exceeded for agent '{agentNameOrId}'.", ex);
                }

                await Task.Delay(delayMs, cancellationToken);
                delayMs *= 2;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AzureAiFoundryClient] Run failed for agent '{Agent}' on attempt {Attempt}",
                    agentNameOrId, attempt);
                throw new FoundryAgentRunFailedException($"Azure AI Foundry agent run failed for '{agentNameOrId}': {ex.Message}", ex);
            }
        }

        throw new FoundryAgentRunFailedException($"Azure AI Foundry agent run failed for '{agentNameOrId}' after {maxRetries} attempts.");
    }

    public async Task<string> ResolveAssistantIdAsync(
        string agentNameOrId,
        CancellationToken cancellationToken = default)
    {
        var def = await GetAgentDefinitionAsync(agentNameOrId, cancellationToken);
        return def.Id;
    }

    public async Task<FoundryAgentDefinition> GetAgentDefinitionAsync(string agentNameOrId, CancellationToken cancellationToken = default)
    {
        if (_agentCache.TryGetValue(agentNameOrId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            return cached.Definition;
        }

        string projectEndpoint = _configuration["AzureAiFoundry:ProjectEndpoint"]?.Trim() ?? string.Empty;
        string apiKey = _configuration["AzureAiFoundry:ApiKey"]?.Trim()
                        ?? _configuration["AzureOpenAI:ApiKey"]?.Trim()
                        ?? string.Empty;

        if (string.IsNullOrWhiteSpace(projectEndpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Azure AI Foundry ProjectEndpoint or ApiKey is missing.");
        }

        string url = $"{projectEndpoint.TrimEnd('/')}/agents/{Uri.EscapeDataString(agentNameOrId)}?api-version=v1";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("api-key", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureAiFoundryClient] Failed to reach Azure AI Foundry endpoint for agent '{Agent}'", agentNameOrId);
            throw new FoundryAgentRunFailedException($"Failed to reach Azure AI Foundry for agent '{agentNameOrId}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("[AzureAiFoundryClient] Agent '{Agent}' not found in Azure AI Foundry project (404).", agentNameOrId);
            throw new FoundryAgentNotFoundException($"Azure AI Foundry agent '{agentNameOrId}' was not found (404).");
        }

        if (response.StatusCode == (System.Net.HttpStatusCode)429)
        {
            _logger.LogWarning("[AzureAiFoundryClient] Rate limit (429) reached while retrieving agent '{Agent}'.", agentNameOrId);
            throw new FoundryRateLimitException($"Azure AI Foundry rate limit exceeded while retrieving agent '{agentNameOrId}'.");
        }

        if (!response.IsSuccessStatusCode)
        {
            string errContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[AzureAiFoundryClient] Azure AI Foundry returned {Status} for agent '{Agent}': {Body}",
                response.StatusCode, agentNameOrId, errContent);
            throw new FoundryAgentRunFailedException($"Azure AI Foundry returned error {(int)response.StatusCode} for agent '{agentNameOrId}'.");
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? agentNameOrId : agentNameOrId;
        string name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? agentNameOrId : agentNameOrId;

        string instructions = string.Empty;
        string defaultModel = _configuration["AzureOpenAI:DeploymentName"]?.Trim() ?? "gpt-5-mini";
        string model = defaultModel;
        string kind = "prompt";

        if (root.TryGetProperty("versions", out var versions) &&
            versions.TryGetProperty("latest", out var latest) &&
            latest.TryGetProperty("definition", out var def))
        {
            if (def.TryGetProperty("instructions", out var instProp))
                instructions = instProp.GetString() ?? string.Empty;
            if (def.TryGetProperty("model", out var modelProp) && !string.IsNullOrWhiteSpace(modelProp.GetString()))
                model = modelProp.GetString()!;
            if (def.TryGetProperty("kind", out var kindProp))
                kind = kindProp.GetString() ?? "prompt";
        }

        var definition = new FoundryAgentDefinition(id, name, instructions, model, kind);
        _agentCache[agentNameOrId] = (definition, DateTime.UtcNow.AddMinutes(2));
        _logger.LogInformation("[AzureAiFoundryClient] Loaded agent '{Agent}' definition from Azure AI Foundry (Kind={Kind}, Model={Model}, InstructionsLength={Length})",
            agentNameOrId, kind, model, instructions.Length);

        return definition;
    }
}
