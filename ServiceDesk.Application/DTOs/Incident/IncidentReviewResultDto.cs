using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Incident;

public record IncidentReviewResultDto(
    bool IsValid,
    bool PrivacyCheckPassed,
    string RedactedTitle,
    string RedactedDescription,
    IncidentPriority CalculatedPriority,
    IReadOnlyList<string> ValidationFeedback
);
