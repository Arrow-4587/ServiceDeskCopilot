using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Services.Agent;

public class AgentWorkflowCoordinator : IAgentWorkflow
{
    private readonly IPlannerService _plannerService;
    private readonly ISupportSpecialistService _specialistService;
    private readonly IIncidentReviewerService _reviewerService;
    private readonly SearchKnowledgeUseCase _searchKnowledgeUseCase;
    private readonly CreateIncidentDraftUseCase _createIncidentDraftUseCase;
    private readonly IIncidentDraftRepository? _draftRepository;
    private readonly IUserContext _userContext;
    private readonly IAiTelemetry _telemetry;
    private readonly ILogger<AgentWorkflowCoordinator> _logger;

    public AgentWorkflowCoordinator(
        IPlannerService plannerService,
        ISupportSpecialistService specialistService,
        IIncidentReviewerService reviewerService,
        SearchKnowledgeUseCase searchKnowledgeUseCase,
        CreateIncidentDraftUseCase createIncidentDraftUseCase,
        IUserContext userContext,
        IAiTelemetry telemetry,
        ILogger<AgentWorkflowCoordinator> logger,
        IIncidentDraftRepository? draftRepository = null)
    {
        _plannerService = plannerService ?? throw new ArgumentNullException(nameof(plannerService));
        _specialistService = specialistService ?? throw new ArgumentNullException(nameof(specialistService));
        _reviewerService = reviewerService ?? throw new ArgumentNullException(nameof(reviewerService));
        _searchKnowledgeUseCase = searchKnowledgeUseCase ?? throw new ArgumentNullException(nameof(searchKnowledgeUseCase));
        _createIncidentDraftUseCase = createIncidentDraftUseCase ?? throw new ArgumentNullException(nameof(createIncidentDraftUseCase));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _draftRepository = draftRepository;
    }

    public async Task<ChatResponseDto> ExecuteWorkflowAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("[AgentWorkflow] Starting Planner -> Specialist -> Reviewer workflow. CorrelationId={CorrelationId}", correlationId);

        // -------------------------------------------------------------
        // STEP 1: Planner Agent (Formulates execution plan)
        // -------------------------------------------------------------
        _telemetry.TrackAgentStep("PlannerAgent", "Analyzing request intent and planning diagnostic steps", correlationId);
        var plan = await _plannerService.PlanAsync(request.UserMessage, cancellationToken);
        _logger.LogInformation("[AgentWorkflow] Planner step completed: Intent={Intent}, Search={Search}, Status={Status}",
            plan.UserIntent, plan.RequiresKnowledgeSearch, plan.RequiresStatusCheck);

        // -------------------------------------------------------------
        // STEP 2: Tool Execution (Dispatched based on Planner decision)
        // -------------------------------------------------------------
        IReadOnlyList<SearchResultDto> knowledgeChunks = Array.Empty<SearchResultDto>();
        if (plan.RequiresKnowledgeSearch && !string.IsNullOrWhiteSpace(plan.SearchQuery))
        {
            _telemetry.TrackToolCall("KnowledgeSearch", true, correlationId);
            knowledgeChunks = await _searchKnowledgeUseCase.ExecuteAsync(new SearchQueryDto(plan.SearchQuery), cancellationToken);
        }

        // Status data is intentionally omitted until an authenticated Azure monitoring integration is configured.
        var statuses = Array.Empty<ServiceDesk.Application.DTOs.Status.ServiceStatusDto>();

        // -------------------------------------------------------------
        // STEP 3: Support Specialist Agent (Troubleshoots with grounded evidence)
        // -------------------------------------------------------------
        _telemetry.TrackAgentStep("SupportSpecialistAgent", "Troubleshooting with retrieved policies and status", correlationId);
        var specialistResult = await _specialistService.TroubleshootAsync(
            request.UserMessage, plan, knowledgeChunks, statuses, cancellationToken);

        bool suggestDraft = specialistResult.SuggestIncidentDraft || plan.RequiresIncidentDraft;
        string? suggestedTitle = specialistResult.SuggestedTitle ?? (suggestDraft ? $"Issue: {Truncate(request.UserMessage, 60)}" : null);
        string? suggestedDescription = specialistResult.SuggestedDescription ?? (suggestDraft ? request.UserMessage : null);
        Guid? createdDraftId = null;

        // -------------------------------------------------------------
        // STEP 4: Reviewer Agent (Audits privacy, completeness, & priority)
        // -------------------------------------------------------------
        if (suggestDraft && !string.IsNullOrWhiteSpace(suggestedTitle) && !string.IsNullOrWhiteSpace(suggestedDescription))
        {
            _telemetry.TrackAgentStep("ReviewerAgent", "Auditing draft incident for privacy sanitization and priority compliance", correlationId);

            var unreviewedDto = new CreateIncidentDraftDto(
                Title: suggestedTitle,
                Description: suggestedDescription,
                Category: specialistResult.Category ?? plan.DraftCategory ?? "General IT",
                Impact: specialistResult.Impact,
                Urgency: specialistResult.Urgency,
                SystemStatusEvidence: statuses.Length > 0 ? string.Join("; ", statuses.Select(s => $"{s.ServiceName}: {s.Status}")) : ""
            );

            var reviewResult = await _reviewerService.ReviewDraftDtoAsync(unreviewedDto, cancellationToken);
            suggestedTitle = reviewResult.RedactedTitle;
            suggestedDescription = reviewResult.RedactedDescription;
        }

        stopwatch.Stop();
        _telemetry.TrackRequestLatency("AgentWorkflowExecution", stopwatch.Elapsed, correlationId);

        return new ChatResponseDto(
            ConversationId: request.ConversationId ?? Guid.NewGuid(),
            Answer: specialistResult.Answer,
            Citations: specialistResult.Citations,
            RequiresClarification: specialistResult.RequiresClarification,
            SuggestedIncidentDraft: suggestDraft,
            SuggestedTitle: suggestedTitle,
            SuggestedDescription: suggestedDescription,
            SuggestedDraftId: createdDraftId
        );
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
