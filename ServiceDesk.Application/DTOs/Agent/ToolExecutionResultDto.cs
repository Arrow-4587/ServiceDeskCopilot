namespace ServiceDesk.Application.DTOs.Agent;

public record ToolExecutionResultDto(
    string ToolName,
    bool Success,
    string Output,
    string? ErrorMessage = null
);
