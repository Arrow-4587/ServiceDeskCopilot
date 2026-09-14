using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Incident;

public record SubmitTicketRequestDto(
    Guid DraftId,
    Guid UserId,
    string Title,
    string Description,
    string Category,
    IncidentPriority Priority,
    string SystemStatusEvidence
);
