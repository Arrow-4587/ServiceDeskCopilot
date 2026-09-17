using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.DTOs.Chat;

public record ConversationSummaryDto(
    Guid Id,
    string Title,
    string FirstUserMessagePreview,
    DateTime LastActivity,
    int MessageCount,
    bool IsActive,
    string? OwnerName = null,
    string? OwnerRole = null
);

public record PaginatedConversationHistoryDto(
    IReadOnlyList<ConversationSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record ChatMessageDetailDto(
    Guid Id,
    string SenderRole,
    string Content,
    DateTime Timestamp,
    IReadOnlyList<CitationDto>? Citations = null,
    bool SuggestedIncidentDraft = false,
    string? SuggestedTitle = null,
    string? SuggestedDescription = null
);

public record ConversationDetailDto(
    Guid Id,
    DateTime StartedAt,
    bool IsActive,
    Guid UserId,
    string? OwnerName,
    IReadOnlyList<ChatMessageDetailDto> Messages
);
