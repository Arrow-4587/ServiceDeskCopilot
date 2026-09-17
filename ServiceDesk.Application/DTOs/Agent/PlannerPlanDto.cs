namespace ServiceDesk.Application.DTOs.Agent;

public record PlannerPlanDto(
    string UserIntent,
    bool RequiresKnowledgeSearch,
    string? SearchQuery,
    bool RequiresStatusCheck,
    string? ServiceName,
    bool RequiresIncidentDraft,
    string? DraftCategory,
    string DiagnosticSummary
);
