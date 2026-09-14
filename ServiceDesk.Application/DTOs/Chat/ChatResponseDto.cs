using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.DTOs.Chat;

public record ChatResponseDto(
    Guid ConversationId,
    string Answer,
    IReadOnlyList<CitationDto> Citations,
    bool RequiresClarification = false,
    bool SuggestedIncidentDraft = false,
    string? SuggestedTitle = null,
    string? SuggestedDescription = null
);
