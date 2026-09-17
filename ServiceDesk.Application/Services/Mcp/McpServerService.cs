using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Mcp;

namespace ServiceDesk.Application.Services.Mcp;

public class McpServerService : IMcpServerService
{
    private readonly IAgentToolDispatcher _dispatcher;
    private readonly ILogger<McpServerService> _logger;

    public McpServerService(IAgentToolDispatcher dispatcher, ILogger<McpServerService> logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<McpToolDto>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var availableTools = _dispatcher.GetAvailableTools();
        var mcpTools = availableTools.Select(t => new McpToolDto(
            Name: FormatMcpToolName(t.Name),
            Description: t.Description,
            InputSchema: new
            {
                type = "object",
                properties = t.Parameters.ToDictionary(
                    kv => kv.Key,
                    kv => new { type = "string", description = kv.Value }
                )
            }
        )).ToList();

        return Task.FromResult<IReadOnlyList<McpToolDto>>(mcpTools);
    }

    public async Task<McpCallToolResultDto> CallToolAsync(McpCallToolRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return new McpCallToolResultDto(
                Content: new[] { new McpContentDto("text", "Error: Request or tool name is required.") },
                IsError: true
            );
        }

        _logger.LogInformation("MCP Server handling tool call: {ToolName}", request.Name);

        var args = request.Arguments ?? new Dictionary<string, string>();
        var result = await _dispatcher.ExecuteToolAsync(request.Name, args, cancellationToken);

        if (!result.Success)
        {
            return new McpCallToolResultDto(
                Content: new[] { new McpContentDto("text", result.ErrorMessage ?? "Tool execution failed.") },
                IsError: true
            );
        }

        return new McpCallToolResultDto(
            Content: new[] { new McpContentDto("text", result.Output) },
            IsError: false
        );
    }

    private static string FormatMcpToolName(string name)
    {
        // Format to snake_case for standard MCP convention (e.g. KnowledgeSearch -> search_knowledge)
        if (string.Equals(name, "KnowledgeSearch", StringComparison.OrdinalIgnoreCase)) return "search_knowledge";
        if (string.Equals(name, "SystemStatus", StringComparison.OrdinalIgnoreCase)) return "get_system_status";
        if (string.Equals(name, "CreateIncidentDraft", StringComparison.OrdinalIgnoreCase)) return "create_incident_draft";
        return name.ToLowerInvariant();
    }
}
