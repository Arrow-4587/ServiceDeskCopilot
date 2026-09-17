using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Services.Agent;

public class AgentToolDispatcher : IAgentToolDispatcher
{
    private readonly SearchKnowledgeUseCase _searchKnowledgeUseCase;
    private readonly GetSystemStatusUseCase _getSystemStatusUseCase;
    private readonly CreateIncidentDraftUseCase _createIncidentDraftUseCase;
    private readonly ILogger<AgentToolDispatcher> _logger;

    public AgentToolDispatcher(
        SearchKnowledgeUseCase searchKnowledgeUseCase,
        GetSystemStatusUseCase getSystemStatusUseCase,
        CreateIncidentDraftUseCase createIncidentDraftUseCase,
        ILogger<AgentToolDispatcher> logger)
    {
        _searchKnowledgeUseCase = searchKnowledgeUseCase ?? throw new ArgumentNullException(nameof(searchKnowledgeUseCase));
        _getSystemStatusUseCase = getSystemStatusUseCase ?? throw new ArgumentNullException(nameof(getSystemStatusUseCase));
        _createIncidentDraftUseCase = createIncidentDraftUseCase ?? throw new ArgumentNullException(nameof(createIncidentDraftUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<ToolDefinitionDto> GetAvailableTools()
    {
        return new List<ToolDefinitionDto>
        {
            new ToolDefinitionDto(
                Name: "KnowledgeSearch",
                Description: "Searches official IT knowledge documents for policies and diagnostic resolution procedures.",
                Parameters: new Dictionary<string, string>
                {
                    { "queryText", "Search keywords or issue topic (string, required)" }
                }
            ),
            new ToolDefinitionDto(
                Name: "SystemStatus",
                Description: "Checks current infrastructure and service operational status.",
                Parameters: new Dictionary<string, string>
                {
                    { "serviceName", "Optional service name to filter (string, e.g. VPN, Outlook, Teams)" }
                }
            ),
            new ToolDefinitionDto(
                Name: "CreateIncidentDraft",
                Description: "Creates a structured incident draft ticket for unresolved IT issues.",
                Parameters: new Dictionary<string, string>
                {
                    { "title", "Short summary title of the issue (string, required)" },
                    { "description", "Detailed description of the issue and troubleshooting steps taken (string, required)" },
                    { "category", "Issue category e.g. Software, Hardware, Network (string, optional)" },
                    { "impact", "Incident Impact: Low, Medium, High, Critical (string, optional)" },
                    { "urgency", "Incident Urgency: Low, Medium, High, Critical (string, optional)" }
                }
            )
        };
    }

    public async Task<ToolExecutionResultDto> ExecuteToolAsync(string toolName, IDictionary<string, string> arguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return new ToolExecutionResultDto(toolName, Success: false, Output: "", ErrorMessage: "Tool name cannot be null or empty.");
        }

        string normalizedTool = toolName.Trim().ToLowerInvariant();
        _logger.LogInformation("Agent Tool Dispatcher executing tool: {ToolName}", toolName);

        try
        {
            switch (normalizedTool)
            {
                case "knowledgesearch":
                    return await ExecuteKnowledgeSearchAsync(arguments, cancellationToken);

                case "systemstatus":
                    return await ExecuteSystemStatusAsync(arguments, cancellationToken);

                case "createincidentdraft":
                    return ExecuteCreateIncidentDraft(arguments);

                default:
                    _logger.LogWarning("Attempted to execute unrecognized tool: {ToolName}", toolName);
                    return new ToolExecutionResultDto(toolName, Success: false, Output: "", ErrorMessage: $"Unrecognized tool '{toolName}'.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent tool: {ToolName}", toolName);
            return new ToolExecutionResultDto(toolName, Success: false, Output: "", ErrorMessage: ex.Message);
        }
    }

    private async Task<ToolExecutionResultDto> ExecuteKnowledgeSearchAsync(IDictionary<string, string> args, CancellationToken cancellationToken)
    {
        string queryText = GetArgValue(args, "queryText") ?? GetArgValue(args, "query") ?? "";
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return new ToolExecutionResultDto("KnowledgeSearch", Success: false, Output: "", ErrorMessage: "Parameter 'queryText' is required.");
        }

        var searchResults = await _searchKnowledgeUseCase.ExecuteAsync(new SearchQueryDto(queryText, TopK: 3, FilterApprovedOnly: true), cancellationToken);
        string jsonOutput = JsonSerializer.Serialize(searchResults, new JsonSerializerOptions { WriteIndented = true });

        return new ToolExecutionResultDto("KnowledgeSearch", Success: true, Output: jsonOutput);
    }

    private async Task<ToolExecutionResultDto> ExecuteSystemStatusAsync(IDictionary<string, string> args, CancellationToken cancellationToken)
    {
        string? serviceName = GetArgValue(args, "serviceName") ?? GetArgValue(args, "service");
        
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            var singleStatus = await _getSystemStatusUseCase.GetByServiceNameAsync(serviceName, cancellationToken);
            string jsonSingle = JsonSerializer.Serialize(singleStatus, new JsonSerializerOptions { WriteIndented = true });
            return new ToolExecutionResultDto("SystemStatus", Success: true, Output: jsonSingle);
        }

        var allStatuses = await _getSystemStatusUseCase.GetAllAsync(cancellationToken);
        string jsonAll = JsonSerializer.Serialize(allStatuses, new JsonSerializerOptions { WriteIndented = true });
        return new ToolExecutionResultDto("SystemStatus", Success: true, Output: jsonAll);
    }

    private ToolExecutionResultDto ExecuteCreateIncidentDraft(IDictionary<string, string> args)
    {
        string? title = GetArgValue(args, "title");
        string? description = GetArgValue(args, "description");
        string category = GetArgValue(args, "category") ?? "General IT";
        string impactStr = GetArgValue(args, "impact") ?? "Low";
        string urgencyStr = GetArgValue(args, "urgency") ?? "Low";

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
        {
            return new ToolExecutionResultDto("CreateIncidentDraft", Success: false, Output: "", ErrorMessage: "Parameters 'title' and 'description' are required.");
        }

        Enum.TryParse<IncidentImpact>(impactStr, true, out var impact);
        Enum.TryParse<IncidentUrgency>(urgencyStr, true, out var urgency);

        var dto = new CreateIncidentDraftDto(title, description, category, impact, urgency);
        var result = _createIncidentDraftUseCase.Execute(dto);

        string jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new ToolExecutionResultDto("CreateIncidentDraft", Success: true, Output: jsonResult);
    }

    private static string? GetArgValue(IDictionary<string, string> args, string key)
    {
        if (args == null) return null;
        var match = args.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
        return match.Key != null ? match.Value : null;
    }
}
