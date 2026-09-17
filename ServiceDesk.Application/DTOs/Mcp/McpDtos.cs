namespace ServiceDesk.Application.DTOs.Mcp;

public record McpToolDto(
    string Name,
    string Description,
    object InputSchema
);

public record McpContentDto(
    string Type,
    string Text
);

public record McpCallToolRequestDto(
    string Name,
    IDictionary<string, string>? Arguments = null
);

public record McpCallToolResultDto(
    IReadOnlyList<McpContentDto> Content,
    bool IsError = false
);
