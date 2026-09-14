namespace ServiceDesk.Application.DTOs.Incident;

public record UserApprovalDecisionDto(
    Guid DraftId,
    bool Approved,
    string? UserNotes
);
