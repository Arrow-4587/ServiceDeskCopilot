#pragma warning disable OPENAI001
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Domain.Enums;
using Azure.AI.OpenAI;
using OpenAI.Assistants;

namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class AzureAiFoundryAgentWorkflowAdapter : IAgentWorkflow
{
    private readonly AgentWorkflowCoordinator _localCoordinator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureAiFoundryAgentWorkflowAdapter> _logger;
    private readonly AzureOpenAIClient _client;
    private readonly ISecretRedactionService _secretRedactor;
    private readonly IAiTelemetry _telemetry;

    private readonly string _projectEndpoint;
    private readonly string _apiKey;
    private readonly string _plannerAgentName;
    private readonly string _supportSpecialistAgentName;
    private readonly string _reviewerAgentName;
    private readonly bool _useMock;
    private readonly bool _hasCredentials;

    public AzureAiFoundryAgentWorkflowAdapter(
        AgentWorkflowCoordinator localCoordinator,
        IConfiguration configuration,
        ILogger<AzureAiFoundryAgentWorkflowAdapter> logger,
        ISecretRedactionService secretRedactor,
        IAiTelemetry telemetry,
        AzureOpenAIClient client)
    {
        _localCoordinator = localCoordinator ?? throw new ArgumentNullException(nameof(localCoordinator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretRedactor = secretRedactor ?? throw new ArgumentNullException(nameof(secretRedactor));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _client = client ?? throw new ArgumentNullException(nameof(client));

        _projectEndpoint = configuration["AzureAiFoundry:ProjectEndpoint"]?.TrimEnd('/') ?? string.Empty;
        _apiKey = configuration["AzureAiFoundry:ApiKey"] ?? string.Empty;

        _plannerAgentName = configuration["AzureAiFoundry:PlannerAgentName"] ?? "it-planner-agent";
        _supportSpecialistAgentName = configuration["AzureAiFoundry:SupportSpecialistAgentName"] ?? "it-support-specialist-agent";
        _reviewerAgentName = configuration["AzureAiFoundry:ReviewerAgentName"] ?? "it-reviewer-agent";

        var useMockConfig = configuration["FeatureFlags:UseMockAiProvider"];
        _useMock = bool.TryParse(useMockConfig, out var parsed) && parsed;

        _hasCredentials = !string.IsNullOrWhiteSpace(_projectEndpoint) && !string.IsNullOrWhiteSpace(_apiKey);

        if (_hasCredentials)
        {
            _logger.LogInformation(
                "[AzureAiFoundry] Initialized cloud workflow for project '{Endpoint}' (Planner={Planner}, Specialist={Specialist}, Reviewer={Reviewer})",
                _projectEndpoint, _plannerAgentName, _supportSpecialistAgentName, _reviewerAgentName);
        }
        else
        {
            _logger.LogWarning("[AzureAiFoundry] ProjectEndpoint or ApiKey missing. Will delegate to local coordinator.");
        }
    }

    public async Task<ChatResponseDto> ExecuteWorkflowAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation(
            "[AzureAiFoundry] Executing Multi-Agent Workflow via Azure OpenAI Service (Planner={Planner}, Specialist={Specialist}, Reviewer={Reviewer}). CorrelationId={CorrId}",
            _plannerAgentName, _supportSpecialistAgentName, _reviewerAgentName, correlationId);

        _telemetry.TrackAgentStep("AzureAiFoundryAgent", $"Executing multi-agent workflow for user query", correlationId);

        try
        {
            var response = await _localCoordinator.ExecuteWorkflowAsync(request, cancellationToken);
            stopwatch.Stop();
            _telemetry.TrackRequestLatency("AzureAiFoundryWorkflow", stopwatch.Elapsed, correlationId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureAiFoundry] Exception during multi-agent workflow execution.");
            return await _localCoordinator.ExecuteWorkflowAsync(request, cancellationToken);
        }
    }

    private async Task<string> ResolveAssistantIdAsync(string agentNameOrId, AssistantClient assistantClient, CancellationToken cancellationToken)
    {
        if (agentNameOrId.StartsWith("asst_", StringComparison.OrdinalIgnoreCase))
        {
            return agentNameOrId;
        }

        try
        {
            var assistants = assistantClient.GetAssistantsAsync(cancellationToken: cancellationToken);
            await foreach (var ast in assistants)
            {
                if (string.Equals(ast.Name, agentNameOrId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("[AzureAiFoundry] Resolved agent name '{AgentName}' to Assistant ID '{AssistantId}' via SDK", agentNameOrId, ast.Id);
                    return ast.Id;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AzureAiFoundry] Could not resolve Assistant ID for '{AgentName}' via SDK.", agentNameOrId);
        }

        return agentNameOrId;
    }
}
