using System.Text;
using System.Text.RegularExpressions;
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
    private readonly IKnowledgeSourceStore? _sourceStore;
    private readonly IKnowledgeIngestionService? _ingestionService;
    private readonly IAuditLogger? _auditLogger;
    private readonly IUserContext? _userContext;

    public KnowledgeController(
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<KnowledgeController> logger,
        IKnowledgeSourceStore? sourceStore = null,
        IKnowledgeIngestionService? ingestionService = null,
        IAuditLogger? auditLogger = null,
        IUserContext? userContext = null)
    {
        _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sourceStore = sourceStore;
        _ingestionService = ingestionService;
        _auditLogger = auditLogger;
        _userContext = userContext;
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

    [HttpPost("UploadPolicyDocument")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<IActionResult> UploadPolicyDocument(
        IFormFile? policyFile,
        [FromForm] string? documentTitle,
        [FromForm] string? section,
        CancellationToken cancellationToken)
    {
        // 1. Validate file presence
        if (policyFile == null || policyFile.Length == 0)
        {
            return BadRequest(new { success = false, message = "Please select a non-empty Markdown (.md) policy file to upload." });
        }

        // 2. Validate max file size (10 MB)
        const long maxSizeBytes = 10 * 1024 * 1024;
        if (policyFile.Length > maxSizeBytes)
        {
            return BadRequest(new { success = false, message = "The selected file exceeds the maximum allowed size of 10 MB." });
        }

        // 3. Validate file extension strictly (.md)
        var rawFileName = policyFile.FileName ?? string.Empty;
        var ext = Path.GetExtension(rawFileName);
        if (!string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Invalid file format. Only Markdown (.md) policy documents are accepted." });
        }

        // 4. Sanitize file name to prevent path traversal or injection
        var sanitizedBaseName = Path.GetFileNameWithoutExtension(rawFileName);
        sanitizedBaseName = Regex.Replace(sanitizedBaseName, @"[^a-zA-Z0-9_\-\s]", "_").Trim();
        if (string.IsNullOrWhiteSpace(sanitizedBaseName))
        {
            sanitizedBaseName = "policy_document";
        }
        sanitizedBaseName = sanitizedBaseName.Replace(' ', '_').ToLowerInvariant();

        // 5. Read and validate text content (must be valid text, not binary)
        string contentString;
        using (var stream = policyFile.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            contentString = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(contentString))
        {
            return BadRequest(new { success = false, message = "The uploaded file is empty or contains only whitespace." });
        }

        if (contentString.Contains('\0'))
        {
            return BadRequest(new { success = false, message = "Binary files are not allowed. Please upload a plain text Markdown document." });
        }

        // 6. Ensure standard YAML frontmatter with Grounding Invariant (Approved: true, Active: true)
        var finalContent = NormalizeMarkdownWithFrontmatter(contentString, sanitizedBaseName, documentTitle, section);

        // 7. Collision-safe filename: e.g. {sanitized}_{timestamp}.md
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var collisionSafeFileName = $"{sanitizedBaseName}_{timestamp}.md";

        if (_sourceStore == null)
        {
            return StatusCode(500, new { success = false, message = "Knowledge source storage service is not configured." });
        }

        try
        {
            using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(finalContent));
            var uploadedDoc = await _sourceStore.UploadDocumentAsync(
                collisionSafeFileName,
                uploadStream,
                "text/markdown",
                cancellationToken);

            int chunksIndexed = 0;
            if (_ingestionService != null)
            {
                try
                {
                    _logger.LogInformation("[KnowledgeController] Triggering immediate knowledge ingestion for uploaded document '{DocName}'...", uploadedDoc.DocumentName);
                    chunksIndexed = await _ingestionService.IngestApprovedKnowledgeDocumentsAsync(cancellationToken);
                }
                catch (Exception ingEx)
                {
                    _logger.LogError(ingEx, "[KnowledgeController] Ingestion error after uploading '{DocName}'. Scheduled worker will retry.", uploadedDoc.DocumentName);
                }
            }

            // Invalidate memory cache so newly uploaded doc is returned immediately
            _knowledgeBaseService.InvalidateCache();

            // Security audit log
            if (_auditLogger != null)
            {
                var userId = _userContext?.UserId.ToString() ?? User?.Identity?.Name ?? "Admin";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                await _auditLogger.LogActionAsync(
                    "UploadPolicyDocument",
                    userId,
                    "Administrator",
                    $"Uploaded policy document '{uploadedDoc.DocumentName}' ({collisionSafeFileName}, {chunksIndexed} chunks indexed)",
                    ip);
            }

            return Ok(new
            {
                success = true,
                message = "Policy document successfully uploaded and indexed.",
                document = uploadedDoc,
                chunksIndexed = chunksIndexed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KnowledgeController] Failed to upload policy document '{FileName}'.", collisionSafeFileName);
            return StatusCode(500, new { success = false, message = "Failed to store document in Azure Blob Storage: " + ex.Message });
        }
    }

    private static string NormalizeMarkdownWithFrontmatter(string content, string defaultId, string? overrideTitle, string? overrideSection)
    {
        var docTitle = !string.IsNullOrWhiteSpace(overrideTitle)
            ? overrideTitle.Trim()
            : defaultId.Replace('_', ' ');
        var docSection = !string.IsNullOrWhiteSpace(overrideSection)
            ? overrideSection.Trim()
            : "General IT Policy";
        var version = "1.0";
        var page = 1;
        var body = content;

        if (content.StartsWith("---"))
        {
            var parts = content.Split(new[] { "---" }, 3, StringSplitOptions.None);
            if (parts.Length >= 3)
            {
                body = parts[2].Trim();
                var yaml = parts[1];
                foreach (var line in yaml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = line.Split(':', 2);
                    if (kv.Length == 2)
                    {
                        var key = kv[0].Trim().ToLowerInvariant();
                        var val = kv[1].Trim().Trim('"', '\'');
                        if (string.IsNullOrWhiteSpace(overrideTitle) && (key == "title" || key == "documentname"))
                        {
                            docTitle = val;
                        }
                        else if (string.IsNullOrWhiteSpace(overrideSection) && key == "section")
                        {
                            docSection = val;
                        }
                        else if (key == "version")
                        {
                            version = val;
                        }
                        else if (key == "page" && int.TryParse(val, out var p))
                        {
                            page = p;
                        }
                    }
                }
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"DocumentName: {docTitle}");
        sb.AppendLine($"Version: {version}");
        sb.AppendLine($"Section: {docSection}");
        sb.AppendLine($"Page: {page}");
        sb.AppendLine("Approved: true");
        sb.AppendLine("Active: true");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(body);

        return sb.ToString();
    }
}
