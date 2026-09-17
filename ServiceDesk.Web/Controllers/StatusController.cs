using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Web.Controllers;

public class StatusController : Controller
{
    private readonly ISystemStatusReader _statusReader;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<StatusController> _logger;

    public StatusController(
        ISystemStatusReader statusReader,
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<StatusController> logger)
    {
        _statusReader = statusReader ?? throw new ArgumentNullException(nameof(statusReader));
        _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceDesk.Application.DTOs.Knowledge.KnowledgeDocumentDto> approvedDocs = Array.Empty<ServiceDesk.Application.DTOs.Knowledge.KnowledgeDocumentDto>();
        try
        {
            approvedDocs = await _knowledgeBaseService.GetApprovedDocumentsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching knowledge documents for Status page.");
        }
        ViewBag.ApprovedDocs = approvedDocs;

        try
        {
            var statuses = await _statusReader.GetAllStatusesAsync(cancellationToken);
            return View(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service status dashboard items.");
            return View(Array.Empty<ServiceDesk.Application.DTOs.Status.ServiceStatusDto>());
        }
    }
}
