using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Incident;

public record IncidentDraftResponseDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Description,
    string Category,
    IncidentImpact Impact,
    IncidentUrgency Urgency,
    IncidentPriority ComputedPriority,
    string SystemStatusEvidence,
    ApprovalStatus ApprovalStatus,
    IncidentStatus Status,
    string? SubmittedIncidentId,
    DateTime CreatedAt,
    DateTime? ApprovedAt
);
