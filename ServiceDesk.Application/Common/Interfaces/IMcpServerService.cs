using ServiceDesk.Application.DTOs.Mcp;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IMcpServerService
{
    Task<IReadOnlyList<McpToolDto>> ListToolsAsync(CancellationToken cancellationToken = default);
    Task<McpCallToolResultDto> CallToolAsync(McpCallToolRequestDto request, CancellationToken cancellationToken = default);
}
