using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.UseCases;

public class ApproveAndSubmitIncidentUseCase
{
    private readonly IUserContext _userContext;
    private readonly IIncidentGateway _incidentGateway;

    public ApproveAndSubmitIncidentUseCase(IUserContext userContext, IIncidentGateway incidentGateway)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _incidentGateway = incidentGateway ?? throw new ArgumentNullException(nameof(incidentGateway));
    }

    public async Task<TicketCreatedResponseDto?> ExecuteAsync(IncidentDraft draft, UserApprovalDecisionDto decision, CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAuthenticated)
            throw new ApplicationAuthorizationException("User must be authenticated to approve or reject incident creation.");

        if (draft.UserId != _userContext.UserId && _userContext.Role == UserRole.Employee)
            throw new ApplicationAuthorizationException("Employees can only approve or submit their own incident drafts.");

        if (!decision.Approved)
        {
            // Explicit rejection by user: DO NOT call ticketing API.
            draft.RejectByAuthenticatedUser();
            return null;
        }

        // Explicit approval by user
        draft.ApproveByAuthenticatedUser();

        var submitRequest = new SubmitTicketRequestDto(
            draft.Id,
            draft.UserId,
            draft.Title,
            draft.Description,
            draft.Category,
            draft.ComputedPriority,
            draft.SystemStatusEvidence
        );

        // Protected write: Invoking the Ticket API Gateway ONLY after explicit user confirmation
        var ticketResponse = await _incidentGateway.CreateTicketAsync(submitRequest, cancellationToken);

        draft.MarkSubmitted(ticketResponse.TicketId);

        return ticketResponse;
    }
}
