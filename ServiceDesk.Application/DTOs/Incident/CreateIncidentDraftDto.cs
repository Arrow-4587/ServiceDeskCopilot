using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Incident;

public record CreateIncidentDraftDto(
    string Title,
    string Description,
    string Category,
    IncidentImpact Impact,
    IncidentUrgency Urgency,
    string SystemStatusEvidence = ""
);
