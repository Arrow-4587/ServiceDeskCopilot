using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Services;

namespace ServiceDesk.Infrastructure.SemanticKernel.Agents;

public class SemanticKernelReviewerAgent
{
    private readonly Kernel _kernel;
    private readonly ISecretRedactionService _redactionService;
    private readonly CreateIncidentDraftUseCase _createIncidentDraftUseCase;
    private readonly IUserContext _userContext;
    private readonly ILogger<SemanticKernelReviewerAgent> _logger;

    public SemanticKernelReviewerAgent(
        Kernel kernel,
        ISecretRedactionService redactionService,
        CreateIncidentDraftUseCase createIncidentDraftUseCase,
        IUserContext userContext,
        ILogger<SemanticKernelReviewerAgent> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _redactionService = redactionService ?? throw new ArgumentNullException(nameof(redactionService));
        _createIncidentDraftUseCase = createIncidentDraftUseCase ?? throw new ArgumentNullException(nameof(createIncidentDraftUseCase));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool IsAudited, string SanitizedTitle, string SanitizedDescription, Guid? CreatedDraftId)> AuditAndPersistDraftAsync(
        string title,
        string description,
        string category,
        string systemEvidence,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SemanticKernel:Reviewer] Auditing incident draft: '{Title}'", title);

        // 1. Privacy & Secret Sanitization
        bool hasTitleSecret = _redactionService.ContainsSecret(title);
        bool hasDescSecret = _redactionService.ContainsSecret(description);

        string sanitizedTitle = _redactionService.RedactSecrets(title);
        string sanitizedDescription = _redactionService.RedactSecrets(description);

        if (hasTitleSecret || hasDescSecret)
        {
            _logger.LogWarning("[SemanticKernel:Reviewer] Sensitive credentials/keys detected and redacted from ticket draft.");
        }

        // 2. Deterministic Priority Evaluation
        var calculatedPriority = DeterministicPriorityCalculator.Calculate(IncidentImpact.Medium, IncidentUrgency.Medium);
        _logger.LogInformation("[SemanticKernel:Reviewer] Deterministic priority calculated: {Priority}", calculatedPriority);

        Guid? createdDraftId = null;

        // 3. Persist Draft for Explicit Human Confirmation if Authenticated
        if (_userContext.IsAuthenticated)
        {
            var draftDto = new CreateIncidentDraftDto(
                Title: sanitizedTitle,
                Description: sanitizedDescription,
                Category: category,
                Impact: IncidentImpact.Medium,
                Urgency: IncidentUrgency.Medium,
                SystemStatusEvidence: systemEvidence
            );

            var created = await _createIncidentDraftUseCase.ExecuteAsync(draftDto, cancellationToken);
            createdDraftId = created.Id;
            _logger.LogInformation("[SemanticKernel:Reviewer] Draft #{DraftId} audited and saved to repository with status PendingApproval.", createdDraftId);
        }

        return (true, sanitizedTitle, sanitizedDescription, createdDraftId);
    }
}
