using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Persistence;

namespace ServiceDesk.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;
    private readonly IUserContext _userContext;
    private readonly IKnowledgeBaseService? _knowledgeBaseService;

    public AdminController(
        ApplicationDbContext dbContext,
        IAuditLogger auditLogger,
        IUserContext userContext,
        IKnowledgeBaseService? knowledgeBaseService = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _knowledgeBaseService = knowledgeBaseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var prompts = await _dbContext.SystemPromptOverrides.AsNoTracking().ToListAsync(cancellationToken);
        var tools = await _dbContext.ToolAllowLists.AsNoTracking().ToListAsync(cancellationToken);
        var auditLogs = await _dbContext.AuditLogs.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(100).ToListAsync(cancellationToken);
        var configs = await _dbContext.AdminConfigs.AsNoTracking().ToListAsync(cancellationToken);

        ServiceDesk.Application.DTOs.Knowledge.KnowledgeDocumentDto[] approvedDocs = Array.Empty<ServiceDesk.Application.DTOs.Knowledge.KnowledgeDocumentDto>();
        if (_knowledgeBaseService != null)
        {
            try
            {
                var docs = await _knowledgeBaseService.GetApprovedDocumentsAsync(cancellationToken);
                approvedDocs = docs.ToArray();
            }
            catch
            {
                // Fallback to empty list if source store is temporarily unavailable
            }
        }

        ViewBag.Prompts = prompts;
        ViewBag.Tools = tools;
        ViewBag.AuditLogs = auditLogs;
        ViewBag.Configs = configs;
        ViewBag.ApprovedDocs = approvedDocs;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddPrompt(UserRole role, string promptText, CancellationToken cancellationToken)
    {
        var prompt = new SystemPromptOverride(role, promptText, _userContext.UserId.ToString());
        _dbContext.SystemPromptOverrides.Add(prompt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogActionAsync("AddSystemPrompt", _userContext.UserId.ToString(), "Administrator", $"Added override for {role}", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
        TempData["SuccessMessage"] = "Prompt override added successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleTool(Guid toolId, bool isAllowed, CancellationToken cancellationToken)
    {
        var tool = await _dbContext.ToolAllowLists.FindAsync(new object[] { toolId }, cancellationToken);
        if (tool != null)
        {
            tool.Toggle(isAllowed, _userContext.UserId.ToString());
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogActionAsync("ToggleTool", _userContext.UserId.ToString(), "Administrator", $"Set {tool.ToolName} to {isAllowed} for {tool.TargetRole}", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            TempData["SuccessMessage"] = "Tool permission updated.";
        }

        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    public async Task<IActionResult> AddTool(UserRole role, string toolName, bool isAllowed, CancellationToken cancellationToken)
    {
        var tool = new ToolAllowList(role, toolName, isAllowed, _userContext.UserId.ToString());
        _dbContext.ToolAllowLists.Add(tool);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogActionAsync("AddTool", _userContext.UserId.ToString(), "Administrator", $"Added {toolName} (Allowed: {isAllowed}) for {role}", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
        TempData["SuccessMessage"] = "Tool added successfully.";

        return RedirectToAction(nameof(Index));
    }
}
