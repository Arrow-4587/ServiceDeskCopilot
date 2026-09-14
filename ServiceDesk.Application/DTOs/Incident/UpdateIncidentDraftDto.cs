using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Incident;

public record UpdateIncidentDraftDto(
    Guid DraftId,
    string Title,
    string Description,
    string Category,
    IncidentImpact Impact,
    IncidentUrgency Urgency
);
