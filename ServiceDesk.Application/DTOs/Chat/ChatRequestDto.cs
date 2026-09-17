namespace ServiceDesk.Application.DTOs.Chat;

public record ChatRequestDto(
    Guid? ConversationId,
    string UserMessage,
    string? CorrelationId = null,
    bool UseAgentWorkflow = false
);
