using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.DTOs.Mcp;
using ServiceDesk.Application.Services.Mcp;

namespace Application.Tests.Mcp;

[TestFixture]
public class McpServerServiceTests
{
    private class FakeDispatcher : IAgentToolDispatcher
    {
        public IReadOnlyList<ToolDefinitionDto> GetAvailableTools()
        {
            return new List<ToolDefinitionDto>
            {
                new ToolDefinitionDto("KnowledgeSearch", "Search docs", new Dictionary<string, string> { { "queryText", "string" } }),
                new ToolDefinitionDto("SystemStatus", "Check status", new Dictionary<string, string> { { "serviceName", "string" } }),
                new ToolDefinitionDto("CreateIncidentDraft", "Create draft", new Dictionary<string, string> { { "title", "string" } })
            };
        }

        public Task<ToolExecutionResultDto> ExecuteToolAsync(string toolName, IDictionary<string, string> arguments, CancellationToken cancellationToken = default)
        {
            if (toolName == "search_knowledge" || toolName == "KnowledgeSearch")
            {
                return Task.FromResult(new ToolExecutionResultDto(toolName, Success: true, Output: "[{ \"Doc\": \"VPN\" }]"));
            }

            return Task.FromResult(new ToolExecutionResultDto(toolName, Success: false, Output: "", ErrorMessage: "Unknown tool"));
        }
    }

    private McpServerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var dispatcher = new FakeDispatcher();
        var logger = NullLogger<McpServerService>.Instance;
        _service = new McpServerService(dispatcher, logger);
    }

    [Test]
    public async Task ListToolsAsync_ReturnsToolsWithMcpFormattedNames()
    {
        // Act
        var tools = await _service.ListToolsAsync();

        // Assert
        Assert.That(tools, Is.Not.Null);
        Assert.That(tools.Count, Is.EqualTo(3));
        Assert.That(tools.Any(t => t.Name == "search_knowledge"), Is.True);
        Assert.That(tools.Any(t => t.Name == "get_system_status"), Is.True);
        Assert.That(tools.Any(t => t.Name == "create_incident_draft"), Is.True);
    }

    [Test]
    public async Task CallToolAsync_ExecutesToolAndReturnsMcpResult()
    {
        // Arrange
        var request = new McpCallToolRequestDto("search_knowledge", new Dictionary<string, string> { { "queryText", "VPN" } });

        // Act
        var result = await _service.CallToolAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Content.Count, Is.EqualTo(1));
        Assert.That(result.Content[0].Text, Contains.Substring("VPN"));
    }

    [Test]
    public async Task CallToolAsync_ReturnsError_WhenToolFailsOrIsUnknown()
    {
        // Arrange
        var request = new McpCallToolRequestDto("unknown_tool", new Dictionary<string, string>());

        // Act
        var result = await _service.CallToolAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsError, Is.True);
        Assert.That(result.Content[0].Text, Contains.Substring("Unknown tool"));
    }
}
