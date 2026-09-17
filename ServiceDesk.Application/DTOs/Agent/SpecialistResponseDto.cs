using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.DTOs.Agent;

public record SpecialistResponseDto(
    string Answer,
    IReadOnlyList<CitationDto> Citations,
    bool RequiresClarification,
    bool SuggestIncidentDraft,
    string? SuggestedTitle,
    string? SuggestedDescription,
    string? Category,
    ServiceDesk.Domain.Enums.IncidentImpact Impact = ServiceDesk.Domain.Enums.IncidentImpact.Medium,
    ServiceDesk.Domain.Enums.IncidentUrgency Urgency = ServiceDesk.Domain.Enums.IncidentUrgency.Medium
);
