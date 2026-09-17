using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Infrastructure.SemanticKernel.Agents;
using ServiceDesk.Infrastructure.SemanticKernel.Plugins;

namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class SemanticKernelAgentWorkflowAdapter : IAgentWorkflow
{
    private readonly SemanticKernelPlannerAgent _plannerAgent;
    private readonly SemanticKernelSpecialistAgent _specialistAgent;
    private readonly SemanticKernelReviewerAgent _reviewerAgent;
    private readonly KnowledgeSearchPlugin _knowledgePlugin;
    private readonly SystemStatusPlugin _statusPlugin;
    private readonly IAiTelemetry _telemetry;
    private readonly ILogger<SemanticKernelAgentWorkflowAdapter> _logger;

    public SemanticKernelAgentWorkflowAdapter(
        SemanticKernelPlannerAgent plannerAgent,
        SemanticKernelSpecialistAgent specialistAgent,
        SemanticKernelReviewerAgent reviewerAgent,
        KnowledgeSearchPlugin knowledgePlugin,
        SystemStatusPlugin statusPlugin,
        IAiTelemetry telemetry,
        ILogger<SemanticKernelAgentWorkflowAdapter> logger)
    {
        _plannerAgent = plannerAgent ?? throw new ArgumentNullException(nameof(plannerAgent));
        _specialistAgent = specialistAgent ?? throw new ArgumentNullException(nameof(specialistAgent));
        _reviewerAgent = reviewerAgent ?? throw new ArgumentNullException(nameof(reviewerAgent));
        _knowledgePlugin = knowledgePlugin ?? throw new ArgumentNullException(nameof(knowledgePlugin));
        _statusPlugin = statusPlugin ?? throw new ArgumentNullException(nameof(statusPlugin));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatResponseDto> ExecuteWorkflowAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("[SemanticKernel:Workflow] Starting Planner -> Specialist -> Reviewer pipeline. CorrelationId={CorrelationId}", correlationId);

        // STEP 1: Planner Agent (Analyze & Decompose)
        _telemetry.TrackAgentStep("SemanticKernelPlannerAgent", "Decomposing query intent and diagnostic requirements", correlationId);
        var plan = await _plannerAgent.PlanAsync(request.UserMessage, cancellationToken);
        _logger.LogInformation("[SemanticKernel:Workflow] Plan formulated: Intent={Intent}, Search={Search}, Status={Status}",
            plan.UserIntent, plan.RequiresKnowledgeSearch, plan.RequiresStatusCheck);

        // STEP 2: Plugin Function Calling
        string? searchResultsJson = null;
        if (plan.RequiresKnowledgeSearch && !string.IsNullOrWhiteSpace(plan.SearchQuery))
        {
            _telemetry.TrackToolCall("KnowledgeSearchPlugin", true, correlationId);
            searchResultsJson = await _knowledgePlugin.SearchPolicyDocumentsAsync(plan.SearchQuery, cancellationToken);
        }

        string? statusJson = null;
        if (plan.RequiresStatusCheck)
        {
            _telemetry.TrackToolCall("SystemStatusPlugin", true, correlationId);
            statusJson = await _statusPlugin.CheckServiceHealthAsync(plan.ServiceName, cancellationToken);
        }

        // STEP 3: Support Specialist Agent (Grounding & Troubleshooting)
        _telemetry.TrackAgentStep("SemanticKernelSpecialistAgent", "Troubleshooting with grounded cloud documents and citations", correlationId);
        var specialistResult = await _specialistAgent.TroubleshootAsync(
            request.UserMessage, plan, searchResultsJson, statusJson, cancellationToken);

        bool suggestDraft = specialistResult.SuggestIncidentDraft || plan.RequiresIncidentDraft;
        string? finalTitle = specialistResult.SuggestedTitle ?? (suggestDraft ? $"Issue: {Truncate(request.UserMessage, 60)}" : null);
        string? finalDescription = specialistResult.SuggestedDescription ?? (suggestDraft ? request.UserMessage : null);
        Guid? createdDraftId = null;

        // STEP 4: Reviewer Agent (Privacy Audit, Completeness, & Priority Check)
        if (suggestDraft && !string.IsNullOrWhiteSpace(finalTitle) && !string.IsNullOrWhiteSpace(finalDescription))
        {
            try
            {
                _telemetry.TrackAgentStep("SemanticKernelReviewerAgent", "Auditing draft incident for privacy sanitization and priority compliance", correlationId);

                var auditResult = await _reviewerAgent.AuditAndPersistDraftAsync(
                    finalTitle,
                    finalDescription,
                    specialistResult.Category ?? plan.DraftCategory ?? "General IT",
                    statusJson ?? "",
                    cancellationToken
                );

                finalTitle = auditResult.SanitizedTitle;
                finalDescription = auditResult.SanitizedDescription;
                createdDraftId = auditResult.CreatedDraftId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SemanticKernel:Workflow] Reviewer agent draft persistence encountered warning. Continuing with response.");
            }
        }

        stopwatch.Stop();
        _telemetry.TrackRequestLatency("SemanticKernelWorkflow", stopwatch.Elapsed, correlationId);

        return new ChatResponseDto(
            ConversationId: request.ConversationId ?? Guid.NewGuid(),
            Answer: specialistResult.Answer,
            Citations: specialistResult.Citations,
            RequiresClarification: specialistResult.RequiresClarification,
            SuggestedIncidentDraft: suggestDraft,
            SuggestedTitle: finalTitle,
            SuggestedDescription: finalDescription,
            SuggestedDraftId: createdDraftId
        );
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
