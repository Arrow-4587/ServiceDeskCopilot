using ServiceDesk.Application.DTOs.Agent;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IAgentToolDispatcher
{
    IReadOnlyList<ToolDefinitionDto> GetAvailableTools();
    Task<ToolExecutionResultDto> ExecuteToolAsync(string toolName, IDictionary<string, string> arguments, CancellationToken cancellationToken = default);
}
