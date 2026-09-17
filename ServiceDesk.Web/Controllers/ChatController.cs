using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Infrastructure.Persistence;
using System.Linq;

namespace ServiceDesk.Web.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IChatConversationService _chatService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserContext _userContext;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatConversationService chatService,
        IConversationRepository conversationRepository,
        IUserContext userContext,
        ApplicationDbContext dbContext,
        ILogger<ChatController> logger)
        : this(chatService, conversationRepository, userContext, dbContext, null!, logger)
    {
    }

    [ActivatorUtilitiesConstructor]
    public ChatController(
        IChatConversationService chatService,
        IConversationRepository conversationRepository,
        IUserContext userContext,
        ApplicationDbContext dbContext,
        IAuditLogger auditLogger,
        ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditLogger = auditLogger;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? conversationId, CancellationToken cancellationToken)
    {
        ConversationSession? session = null;
        if (conversationId.HasValue && conversationId.Value != Guid.Empty)
        {
            session = await _conversationRepository.GetByIdAsync(conversationId.Value, cancellationToken);
            if (session == null)
            {
                return NotFound();
            }

            if (_userContext.Role == ServiceDesk.Domain.Enums.UserRole.Employee && session.UserId != _userContext.UserId)
            {
                return Forbid();
            }
        }

        Guid? filterUserId = _userContext.Role == ServiceDesk.Domain.Enums.UserRole.Employee ? _userContext.UserId : (Guid?)null;
        var pagedHistory = await _conversationRepository.GetPagedSummariesAsync(filterUserId, 1, 10, null, null, cancellationToken);
        var userHistory = await _conversationRepository.GetByUserIdAsync(_userContext.UserId, cancellationToken);

        ViewBag.HistoryPaged = pagedHistory;
        ViewBag.SidebarHistory = userHistory.OrderByDescending(s => s.StartedAt).ToList();
        ViewBag.ActiveConversationId = session?.Id ?? Guid.Empty;
        return View(session);
    }

    [HttpGet]
    public async Task<IActionResult> HistoryApi(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? timeFilter = null,
        CancellationToken cancellationToken = default)
    {
        Guid? filterUserId = _userContext.Role == ServiceDesk.Domain.Enums.UserRole.Employee ? _userContext.UserId : (Guid?)null;
        var paged = await _conversationRepository.GetPagedSummariesAsync(filterUserId, page, pageSize, search, timeFilter, cancellationToken);
        return Json(paged);
    }

    [HttpGet]
    [Route("Chat/Conversation/{id:guid}")]
    public async Task<IActionResult> GetConversationApi(Guid id, CancellationToken cancellationToken)
    {
        var session = await _conversationRepository.GetByIdAsync(id, cancellationToken);
        if (session == null)
        {
            return NotFound(new { error = "Conversation not found." });
        }

        if (_userContext.Role == ServiceDesk.Domain.Enums.UserRole.Employee && session.UserId != _userContext.UserId)
        {
            return Forbid();
        }

        var messageDtos = session.Messages
            .OrderBy(m => m.Timestamp)
            .Select(m => new ChatMessageDetailDto(
                m.Id,
                m.SenderRole,
                m.Content,
                m.Timestamp,
                m.Citations?.Select(c => new ServiceDesk.Application.DTOs.Knowledge.CitationDto(c.DocumentName, c.Section, c.Page, c.Snippet, c.BlobPath)).ToList()
            ))
            .ToList();

        var detail = new ConversationDetailDto(
            session.Id,
            session.StartedAt,
            session.IsActive,
            session.UserId,
            _userContext.Username,
            messageDtos
        );

        return Json(detail);
    }

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        IReadOnlyList<ConversationSession> sessions;
        if (_userContext.Role == ServiceDesk.Domain.Enums.UserRole.Employee)
        {
            sessions = await _conversationRepository.GetByUserIdAsync(_userContext.UserId, cancellationToken);
        }
        else
        {
            sessions = await _conversationRepository.GetAllAsync(cancellationToken);
        }

        var userIds = sessions.Select(s => s.UserId).Distinct().ToList();
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Role, cancellationToken);

        var historyItems = sessions.Select(s => new {
            Session = s,
            Role = users.ContainsKey(s.UserId) ? users[s.UserId].ToString() : "Unknown"
        }).ToList();

        ViewBag.HistoryItems = historyItems;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return BadRequest(new { error = "User message is required." });
        }

        try
        {
            var response = await _chatService.ProcessChatMessageAsync(request, cancellationToken);

            if (_auditLogger != null)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var truncatedPrompt = request.UserMessage.Length > 60 ? request.UserMessage[..60] + "..." : request.UserMessage;
                await _auditLogger.LogActionAsync("ChatQuery", _userContext.Username, _userContext.Role.ToString(), $"Query: '{truncatedPrompt}'", ip, cancellationToken);
            }

            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message.");
            return StatusCode(500, new { error = "Failed to process chat message." });
        }
    }
}
