using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.UseCases;

public class CreateIncidentDraftUseCase
{
    private readonly IUserContext _userContext;

    public CreateIncidentDraftUseCase(IUserContext userContext)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public IncidentDraftResponseDto Execute(CreateIncidentDraftDto dto)
    {
        if (!_userContext.IsAuthenticated)
            throw new ApplicationAuthorizationException("User must be authenticated to create an incident draft.");

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.Title))
            errors.Add(nameof(dto.Title), new[] { "Title is required." });
        if (string.IsNullOrWhiteSpace(dto.Description))
            errors.Add(nameof(dto.Description), new[] { "Description is required." });

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var draft = new IncidentDraft(
            _userContext.UserId,
            dto.Title,
            dto.Description,
            dto.Category,
            dto.Impact,
            dto.Urgency,
            dto.SystemStatusEvidence
        );

        return new IncidentDraftResponseDto(
            draft.Id,
            draft.UserId,
            draft.Title,
            draft.Description,
            draft.Category,
            draft.Impact,
            draft.Urgency,
            draft.ComputedPriority,
            draft.SystemStatusEvidence,
            draft.ApprovalStatus,
            draft.Status,
            draft.SubmittedIncidentId,
            draft.CreatedAt,
            draft.ApprovedAt
        );
    }
}
