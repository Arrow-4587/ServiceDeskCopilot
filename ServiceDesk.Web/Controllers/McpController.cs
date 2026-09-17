using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Mcp;

namespace ServiceDesk.Web.Controllers;

[ApiController]
[Route("api/mcp")]
public class McpController : ControllerBase
{
    private readonly IMcpServerService _mcpService;

    public McpController(IMcpServerService mcpService)
    {
        _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
    }

    [HttpGet("tools")]
    public async Task<IActionResult> ListTools(CancellationToken cancellationToken)
    {
        var tools = await _mcpService.ListToolsAsync(cancellationToken);
        return Ok(new { tools });
    }

    [HttpPost("call")]
    public async Task<IActionResult> CallTool([FromBody] McpCallToolRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new McpCallToolResultDto(
                Content: new[] { new McpContentDto("text", "Invalid request body.") },
                IsError: true
            ));
        }

        var result = await _mcpService.CallToolAsync(request, cancellationToken);
        if (result.IsError)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
