namespace ServiceDesk.Application.DTOs.Agent;

public record ToolDefinitionDto(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Parameters
);
