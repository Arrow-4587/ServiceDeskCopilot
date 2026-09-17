using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Web.Controllers;

[Authorize]
public class IncidentController : Controller
{
    private readonly IIncidentDraftRepository _draftRepository;
    private readonly IIncidentReviewerService _reviewerService;
    private readonly ApproveAndSubmitIncidentUseCase _approveAndSubmitUseCase;
    private readonly IUserContext _userContext;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<IncidentController> _logger;

    public IncidentController(
        IIncidentDraftRepository draftRepository,
        IIncidentReviewerService reviewerService,
        ApproveAndSubmitIncidentUseCase approveAndSubmitUseCase,
        IUserContext userContext,
        ILogger<IncidentController> logger)
        : this(draftRepository, reviewerService, approveAndSubmitUseCase, userContext, null!, logger)
    {
    }

    [ActivatorUtilitiesConstructor]
    public IncidentController(
        IIncidentDraftRepository draftRepository,
        IIncidentReviewerService reviewerService,
        ApproveAndSubmitIncidentUseCase approveAndSubmitUseCase,
        IUserContext userContext,
        IAuditLogger auditLogger,
        ILogger<IncidentController> logger)
    {
        _draftRepository = draftRepository ?? throw new ArgumentNullException(nameof(draftRepository));
        _reviewerService = reviewerService ?? throw new ArgumentNullException(nameof(reviewerService));
        _approveAndSubmitUseCase = approveAndSubmitUseCase ?? throw new ArgumentNullException(nameof(approveAndSubmitUseCase));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _auditLogger = auditLogger;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<IncidentDraft> drafts;
        if (!_userContext.IsAuthenticated)
        {
            drafts = Array.Empty<IncidentDraft>();
        }
        else
        {
            drafts = await _draftRepository.GetByUserIdAsync(_userContext.UserId, cancellationToken);
        }

        return View(drafts);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(id, cancellationToken);
        if (draft == null)
        {
            return NotFound("Incident draft not found.");
        }

        var reviewResult = await _reviewerService.ReviewDraftAsync(draft, cancellationToken);
        ViewBag.ReviewResult = reviewResult;

        return View(draft);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveOrReject(Guid draftId, bool approved, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(draftId, cancellationToken);
        if (draft == null)
        {
            return NotFound("Incident draft not found.");
        }

        // P1: Security Guard (Employee Ownership)
        if (_userContext.Role == UserRole.Employee && draft.UserId != _userContext.UserId)
        {
            return Forbid();
        }

        // Only allow Analyst to approve if assigned to them? Actually, let's just make sure they are an Analyst or Employee.
        if (_userContext.Role == UserRole.Analyst && draft.AssignedAnalystId != _userContext.UserId && draft.AssignedAnalystId != null)
        {
            // Optional: require assignment first, but let's just let any analyst approve unassigned, or assigned to them.
            if (draft.AssignedAnalystId != _userContext.UserId) 
                return Forbid();
        }

        try
        {
            var decision = new UserApprovalDecisionDto(draftId, approved, null);
            var result = await _approveAndSubmitUseCase.ExecuteAsync(draft, decision, cancellationToken);
            await _draftRepository.UpdateAsync(draft, cancellationToken);

            if (_auditLogger != null)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var actionName = approved ? "ApproveIncident" : "RejectIncident";
                var details = approved 
                    ? $"Approved incident '{draft.Title}' (Ticket: {result?.TicketId ?? "N/A"})" 
                    : $"Rejected incident draft '{draft.Title}'";
                await _auditLogger.LogActionAsync(actionName, _userContext.Username, _userContext.Role.ToString(), details, ip, cancellationToken);
            }

            if (approved && result != null)
            {
                TempData["SuccessMessage"] = $"Ticket '{result.TicketId}' successfully submitted to ITSM Gateway!";
            }
            else
            {
                TempData["WarningMessage"] = "Incident draft was rejected and will not be submitted to ITSM Gateway.";
            }

            return RedirectToAction(nameof(Details), new { id = draftId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval decision for draft {DraftId}", draftId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = draftId });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Analyst,Administrator")]
    public async Task<IActionResult> AssignToMe(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(draftId, cancellationToken);
        if (draft == null)
            return NotFound();

        try
        {
            draft.AssignTo(_userContext.UserId);
            await _draftRepository.UpdateAsync(draft, cancellationToken);
            TempData["SuccessMessage"] = "Draft assigned to you.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = draftId });
    }

    [HttpPost]
    [Authorize(Roles = "Analyst,Administrator")]
    public async Task<IActionResult> UpdateDraft(Guid draftId, string title, string description, string category, IncidentImpact impact, IncidentUrgency urgency, string? feedback, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(draftId, cancellationToken);
        if (draft == null)
            return NotFound();

        // Ensure assigned to this analyst
        if (draft.AssignedAnalystId != _userContext.UserId)
            return Forbid();

        try
        {
            draft.UpdateDraft(title, description, impact, urgency, category);
            
            if (!string.IsNullOrWhiteSpace(feedback))
            {
                draft.ProvideFeedback(feedback);
            }

            await _draftRepository.UpdateAsync(draft, cancellationToken);
            TempData["SuccessMessage"] = "Draft updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = draftId });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraftManual(
        [FromBody] ManualDraftRequest request,
        [FromServices] CreateIncidentDraftUseCase createIncidentDraftUseCase,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "Title and description are required." });
        }

        var dto = new CreateIncidentDraftDto(
            Title: request.Title,
            Description: request.Description,
            Category: "General IT",
            Impact: IncidentImpact.Medium,
            Urgency: IncidentUrgency.Medium,
            SystemStatusEvidence: ""
        );

        var created = await createIncidentDraftUseCase.ExecuteAsync(dto, cancellationToken);
        return Json(new { draftId = created.Id });
    }

    public class ManualDraftRequest 
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
