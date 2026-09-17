using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.ValueObjects;

namespace ServiceDesk.Application.Services.Chat;

public class ChatConversationService : IChatConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IRagGroundingService _ragGroundingService;
    private readonly CreateIncidentDraftUseCase _createIncidentDraftUseCase;
    private readonly IUserContext _userContext;
    private readonly IAgentWorkflow? _agentWorkflow;
    private readonly ILogger<ChatConversationService> _logger;

    public ChatConversationService(
        IConversationRepository conversationRepository,
        IRagGroundingService ragGroundingService,
        CreateIncidentDraftUseCase createIncidentDraftUseCase,
        IUserContext userContext,
        ILogger<ChatConversationService> logger,
        IAgentWorkflow? agentWorkflow = null)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _ragGroundingService = ragGroundingService ?? throw new ArgumentNullException(nameof(ragGroundingService));
        _createIncidentDraftUseCase = createIncidentDraftUseCase ?? throw new ArgumentNullException(nameof(createIncidentDraftUseCase));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentWorkflow = agentWorkflow;
    }

    public async Task<ChatResponseDto> ProcessChatMessageAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            throw new ArgumentException("Chat request or user message cannot be empty.", nameof(request));
        }

        // 1. Get or Create Session
        bool isNewSession = false;
        ConversationSession? session = null;

        if (request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty)
        {
            session = await _conversationRepository.GetByIdAsync(request.ConversationId.Value, cancellationToken);
        }

        if (session == null)
        {
            isNewSession = true;
            Guid activeUserId = _userContext.IsAuthenticated ? _userContext.UserId : Guid.NewGuid();
            session = new ConversationSession(activeUserId);
        }

        // 2. Add User Message to Session History
        session.AddMessage("User", request.UserMessage);

        // Optional Multi-Agent Workflow Delegation
        if (request.UseAgentWorkflow && _agentWorkflow != null)
        {
            _logger.LogInformation("Routing chat request to multi-agent workflow (Planner -> Specialist -> Reviewer).");
            var agentResult = await _agentWorkflow.ExecuteWorkflowAsync(request, cancellationToken);

            var citations = agentResult.Citations
                .Select(c => new Citation(c.DocumentName, c.Section, c.Page, c.Snippet, c.BlobPath))
                .ToList();

            session.AddMessage("Assistant", agentResult.Answer, citations);
            await SaveSessionAsync(session, isNewSession, cancellationToken);

            return agentResult with { ConversationId = session.Id };
        }

        bool explicitTicketRequest = IsExplicitTicketRequest(request.UserMessage);

        // 3. Process RAG Grounding unless explicitly requesting a ticket
        RagAnswerResultDto? ragResult = null;
        if (!explicitTicketRequest)
        {
            ragResult = await _ragGroundingService.GenerateGroundedAnswerAsync(request.UserMessage, cancellationToken);
        }

        // 4. Case A: Grounded Knowledge Answer Found
        if (ragResult != null && ragResult.Grounded && !ragResult.TriggeredFallback)
        {
            var domainCitations = ragResult.Citations
                .Select(c => new Citation(c.DocumentName, c.Section, c.Page, c.Snippet, c.BlobPath))
                .ToList();

            session.AddMessage("Assistant", ragResult.Answer, domainCitations);
            await SaveSessionAsync(session, isNewSession, cancellationToken);

            return new ChatResponseDto(
                ConversationId: session.Id,
                Answer: ragResult.Answer,
                Citations: ragResult.Citations,
                RequiresClarification: false,
                SuggestedIncidentDraft: false
            );
        }

        // 5. Case B: Fallback or Explicit Ticket Request -> Propose Incident Draft
        string responseAnswer;
        bool suggestDraft = false;
        string? suggestedTitle = null;
        string? suggestedDescription = null;

        Guid? createdDraftId = null;

        if (_userContext.IsAuthenticated)
        {
            suggestedTitle = TruncateString($"Issue: {request.UserMessage}", 80);
            suggestedDescription = request.UserMessage;

            var draftDto = new CreateIncidentDraftDto(
                Title: suggestedTitle,
                Description: suggestedDescription,
                Category: "General IT",
                Impact: IncidentImpact.Low,
                Urgency: IncidentUrgency.Low,
                SystemStatusEvidence: "RAG ground search returned 0 approved knowledge documents."
            );

            var createdDraft = await _createIncidentDraftUseCase.ExecuteAsync(draftDto, cancellationToken);
            createdDraftId = createdDraft.Id;
            suggestDraft = true;

            responseAnswer = $"I couldn't find an approved resolution in our IT knowledge base for your issue. I have created a draft incident ticket titled '{createdDraft.Title}' for your review. Would you like to review and submit it?";
        }
        else
        {
            responseAnswer = "I'm sorry, but I couldn't find an approved answer in our IT knowledge base. Please sign in to automatically create an IT incident ticket.";
        }

        session.AddMessage("Assistant", responseAnswer);
        await SaveSessionAsync(session, isNewSession, cancellationToken);

        return new ChatResponseDto(
            ConversationId: session.Id,
            Answer: responseAnswer,
            Citations: Array.Empty<CitationDto>(),
            RequiresClarification: false,
            SuggestedIncidentDraft: suggestDraft,
            SuggestedTitle: suggestedTitle,
            SuggestedDescription: suggestedDescription,
            SuggestedDraftId: createdDraftId
        );
    }

    private Task SaveSessionAsync(ConversationSession session, bool isNewSession, CancellationToken cancellationToken)
    {
        if (isNewSession)
        {
            return _conversationRepository.AddAsync(session, cancellationToken);
        }
        return _conversationRepository.UpdateAsync(session, cancellationToken);
    }

    private static bool IsExplicitTicketRequest(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        string lower = message.ToLowerInvariant();
        return lower.Contains("create ticket") ||
               lower.Contains("create a ticket") ||
               lower.Contains("raise ticket") ||
               lower.Contains("raise a ticket") ||
               lower.Contains("open ticket") ||
               lower.Contains("open a ticket") ||
               lower.Contains("create incident") ||
               lower.Contains("submit ticket") ||
               lower.Contains("submit a ticket") ||
               lower.Contains("file a ticket") ||
               lower.Contains("log a ticket") ||
               (lower.Contains("ticket") && (lower.Contains("please") || lower.Contains("need") || lower.Contains("want") || lower.Contains("can you") || lower.Contains("create") || lower.Contains("open")));
    }

    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input.Length <= maxLength ? input : input.Substring(0, maxLength - 3) + "...";
    }
}
