using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Web.Controllers;

[Authorize]
[Route("[controller]")]
public class KnowledgeController : Controller
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<KnowledgeController> _logger;

    public KnowledgeController(
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<KnowledgeController> logger)
    {
        _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var documents = await _knowledgeBaseService.GetApprovedDocumentsAsync(cancellationToken);
            return View(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KnowledgeController] Failed to retrieve knowledge documents from Azure Blob Storage.");
            ViewBag.ErrorMessage = "Azure Blob Storage knowledge source is currently unavailable. " + ex.Message;
            return View(Array.Empty<KnowledgeDocumentDto>());
        }
    }

    [HttpGet("GetDocuments")]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        try
        {
            var documents = await _knowledgeBaseService.GetApprovedDocumentsAsync(cancellationToken);
            return Json(new { success = true, documents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KnowledgeController] Error fetching knowledge documents from Azure Blob Storage.");
            return StatusCode(500, new { success = false, message = "Azure Blob Storage knowledge source is currently unavailable.", error = ex.Message });
        }
    }

    [HttpGet("Document/{id}")]
    public async Task<IActionResult> GetDocument(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { success = false, message = "Document ID is required." });
        }

        try
        {
            var detail = await _knowledgeBaseService.GetDocumentDetailAsync(id, cancellationToken);
            if (detail == null)
            {
                return NotFound(new { success = false, message = $"Approved document '{id}' was not found." });
            }

            return Json(new { success = true, data = detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KnowledgeController] Error retrieving document detail for '{Id}'.", id);
            return StatusCode(500, new { success = false, message = "Azure Blob Storage knowledge source is currently unavailable.", error = ex.Message });
        }
    }

    [HttpGet("Content/{id}")]
    public async Task<IActionResult> GetContent(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { success = false, message = "Document ID is required." });
        }

        try
        {
            var detail = await _knowledgeBaseService.GetDocumentDetailAsync(id, cancellationToken);
            if (detail == null)
            {
                return NotFound(new { success = false, message = $"Approved document '{id}' was not found." });
            }

            return Content(detail.RenderedHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KnowledgeController] Error retrieving document content for '{Id}'.", id);
            return StatusCode(500, "Azure Blob Storage knowledge source is currently unavailable.");
        }
    }
}
