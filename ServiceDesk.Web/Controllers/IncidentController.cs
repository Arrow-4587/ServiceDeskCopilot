using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Web.Controllers;

public class IncidentController : Controller
{
    private readonly IIncidentDraftRepository _draftRepository;
    private readonly IIncidentReviewerService _reviewerService;
    private readonly ApproveAndSubmitIncidentUseCase _approveAndSubmitUseCase;
    private readonly IUserContext _userContext;
    private readonly ILogger<IncidentController> _logger;

    public IncidentController(
        IIncidentDraftRepository draftRepository,
        IIncidentReviewerService reviewerService,
        ApproveAndSubmitIncidentUseCase approveAndSubmitUseCase,
        IUserContext userContext,
        ILogger<IncidentController> logger)
    {
        _draftRepository = draftRepository ?? throw new ArgumentNullException(nameof(draftRepository));
        _reviewerService = reviewerService ?? throw new ArgumentNullException(nameof(reviewerService));
        _approveAndSubmitUseCase = approveAndSubmitUseCase ?? throw new ArgumentNullException(nameof(approveAndSubmitUseCase));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
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

        try
        {
            var decision = new UserApprovalDecisionDto(draftId, approved, null);
            var result = await _approveAndSubmitUseCase.ExecuteAsync(draft, decision, cancellationToken);
            await _draftRepository.UpdateAsync(draft, cancellationToken);

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
}
