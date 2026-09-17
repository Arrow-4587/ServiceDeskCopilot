using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Services;

namespace ServiceDesk.Application.Services.Agent;

public class IncidentReviewerService : IIncidentReviewerService
{
    private readonly ISecretRedactionService _redactionService;
    private readonly ILogger<IncidentReviewerService> _logger;

    public IncidentReviewerService(
        ISecretRedactionService redactionService,
        ILogger<IncidentReviewerService> logger)
    {
        _redactionService = redactionService ?? throw new ArgumentNullException(nameof(redactionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IncidentReviewResultDto> ReviewDraftAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
    {
        if (draft == null) throw new ArgumentNullException(nameof(draft));

        var dto = new CreateIncidentDraftDto(
            Title: draft.Title,
            Description: draft.Description,
            Category: draft.Category,
            Impact: draft.Impact,
            Urgency: draft.Urgency,
            SystemStatusEvidence: draft.SystemStatusEvidence
        );

        return ReviewDraftDtoAsync(dto, cancellationToken);
    }

    public Task<IncidentReviewResultDto> ReviewDraftDtoAsync(CreateIncidentDraftDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var feedback = new List<string>();

        // 1. Completeness Validation
        bool titleValid = !string.IsNullOrWhiteSpace(dto.Title) && dto.Title.Trim().Length >= 5;
        if (!titleValid)
        {
            feedback.Add("Title is incomplete or too short (minimum 5 characters required).");
        }

        bool descValid = !string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Trim().Length >= 15;
        if (!descValid)
        {
            feedback.Add("Description is incomplete or too brief (minimum 15 characters required).");
        }

        if (string.IsNullOrWhiteSpace(dto.Category))
        {
            feedback.Add("Category must be specified.");
        }

        // 2. Privacy & Secret Inspection
        bool hasTitleSecret = _redactionService.ContainsSecret(dto.Title);
        bool hasDescSecret = _redactionService.ContainsSecret(dto.Description);
        bool privacyPassed = !hasTitleSecret && !hasDescSecret;

        string redactedTitle = _redactionService.RedactSecrets(dto.Title);
        string redactedDescription = _redactionService.RedactSecrets(dto.Description);

        if (!privacyPassed)
        {
            feedback.Add("Privacy compliance warning: Sensitive credentials/keys detected and redacted from ticket content.");
            _logger.LogWarning("Reviewer Agent detected and sanitized credentials in incident draft title/description.");
        }

        // 3. Priority Recalculation & Alignment Check
        IncidentPriority calculatedPriority = DeterministicPriorityCalculator.Calculate(dto.Impact, dto.Urgency);
        feedback.Add($"Priority alignment verified: Impact ({dto.Impact}) + Urgency ({dto.Urgency}) => Calculated Priority ({calculatedPriority}).");

        bool isValid = titleValid && descValid && !string.IsNullOrWhiteSpace(dto.Category);

        var result = new IncidentReviewResultDto(
            IsValid: isValid,
            PrivacyCheckPassed: privacyPassed,
            RedactedTitle: redactedTitle,
            RedactedDescription: redactedDescription,
            CalculatedPriority: calculatedPriority,
            ValidationFeedback: feedback
        );

        return Task.FromResult(result);
    }
}
