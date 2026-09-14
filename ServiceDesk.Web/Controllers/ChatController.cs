using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Web.Controllers;

public class ChatController : Controller
{
    private readonly IChatConversationService _chatService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserContext _userContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatConversationService chatService,
        IConversationRepository conversationRepository,
        IUserContext userContext,
        ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
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

        ViewBag.ActiveConversationId = session?.Id ?? Guid.Empty;
        return View(session);
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
            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message.");
            return StatusCode(500, new { error = "Failed to process chat message." });
        }
    }
}
