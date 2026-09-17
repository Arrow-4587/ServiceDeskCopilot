using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        }

        // Load sidebar history
        var history = await _conversationRepository.GetByUserIdAsync(_userContext.UserId, cancellationToken);
        ViewBag.SidebarHistory = history.OrderByDescending(s => s.StartedAt).ToList();
        ViewBag.ActiveConversationId = session?.Id ?? Guid.Empty;
        return View(session);
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
        var users = _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Role);

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
