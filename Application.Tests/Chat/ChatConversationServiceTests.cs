using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Services.Chat;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace Application.Tests.Chat;

[TestFixture]
public class ChatConversationServiceTests
{
    private class FakeConversationRepository : IConversationRepository
    {
        private readonly Dictionary<Guid, ConversationSession> _store = new();

        public Task<ConversationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(id, out var session);
            return Task.FromResult(session);
        }

        public Task AddAsync(ConversationSession session, CancellationToken cancellationToken = default)
        {
            _store[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConversationSession session, CancellationToken cancellationToken = default)
        {
            _store[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ConversationSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var list = _store.Values.Where(s => s.UserId == userId).ToList();
            return Task.FromResult<IReadOnlyList<ConversationSession>>(list);
        }

        public Task<IReadOnlyList<ConversationSession>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var list = _store.Values.ToList();
            return Task.FromResult<IReadOnlyList<ConversationSession>>(list);
        }

        public Task<PaginatedConversationHistoryDto> GetPagedSummariesAsync(
            Guid? userId,
            int page,
            int pageSize,
            string? search,
            string? timeFilter,
            CancellationToken cancellationToken = default)
        {
            var query = _store.Values.AsQueryable();
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(s => s.Messages.Any(m => m.Content.ToLower().Contains(searchLower)));
            }

            var totalCount = query.Count();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedSessions = query
                .OrderByDescending(s => s.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var items = pagedSessions.Select(s => {
                var firstMsg = s.Messages.FirstOrDefault();
                var firstUserMsg = s.Messages.FirstOrDefault(m => m.SenderRole == "User");
                var lastMsg = s.Messages.LastOrDefault();
                return new ConversationSummaryDto(
                    s.Id,
                    firstMsg != null ? firstMsg.Content : "New Conversation",
                    firstUserMsg != null ? firstUserMsg.Content : "",
                    lastMsg != null ? lastMsg.Timestamp : s.StartedAt,
                    s.Messages.Count,
                    s.IsActive
                );
            }).ToList();

            return Task.FromResult(new PaginatedConversationHistoryDto(items, page, pageSize, totalCount, totalPages));
        }
    }

    private class FakeRagService : IRagGroundingService
    {
        public bool ReturnFallback { get; set; } = false;

        public Task<RagAnswerResultDto> GenerateGroundedAnswerAsync(string userQuery, CancellationToken cancellationToken = default)
        {
            if (ReturnFallback)
            {
                return Task.FromResult(new RagAnswerResultDto("Fallback message", Array.Empty<CitationDto>(), Grounded: false, TriggeredFallback: true));
            }

            var citations = new List<CitationDto>
            {
                new CitationDto("VPN Guide", "General", 1, "Connect to VPN.", "data/knowledge/vpn.md")
            };

            return Task.FromResult(new RagAnswerResultDto("To connect, open Cisco AnyConnect.", citations, Grounded: true, TriggeredFallback: false));
        }
    }

    private class FakeUserContext : IUserContext
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username => "jdoe";
        public string Email => "user@company.com";
        public UserRole Role => UserRole.Employee;
        public bool IsAuthenticated { get; set; } = true;
    }

    private ChatConversationService _service = null!;
    private FakeConversationRepository _repo = null!;
    private FakeRagService _rag = null!;
    private FakeUserContext _userContext = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new FakeConversationRepository();
        _rag = new FakeRagService();
        _userContext = new FakeUserContext();

        var draftUseCase = new CreateIncidentDraftUseCase(_userContext);
        var logger = NullLogger<ChatConversationService>.Instance;

        _service = new ChatConversationService(_repo, _rag, draftUseCase, _userContext, logger);
    }

    [Test]
    public async Task ProcessChatMessageAsync_ReturnsGroundedAnswerAndCitations_WhenKBMatches()
    {
        // Arrange
        var request = new ChatRequestDto(Guid.Empty, "How do I connect to VPN?");

        // Act
        var response = await _service.ProcessChatMessageAsync(request);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.SuggestedIncidentDraft, Is.False);
        Assert.That(response.Answer, Contains.Substring("Cisco AnyConnect"));
        Assert.That(response.Citations.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ProcessChatMessageAsync_ProposesIncidentDraft_WhenFallbackTriggersAndUserIsAuthenticated()
    {
        // Arrange
        _rag.ReturnFallback = true;
        var request = new ChatRequestDto(Guid.Empty, "My monitor has vertical blue lines");

        // Act
        var response = await _service.ProcessChatMessageAsync(request);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.SuggestedIncidentDraft, Is.True);
        Assert.That(response.SuggestedTitle, Is.Not.Null);
        Assert.That(response.SuggestedDescription, Is.EqualTo("My monitor has vertical blue lines"));
    }

    [Test]
    public async Task ProcessChatMessageAsync_PersistsMultiTurnHistoryInConversationRepository()
    {
        // Arrange
        var request1 = new ChatRequestDto(Guid.Empty, "Hello, VPN question.");
        var response1 = await _service.ProcessChatMessageAsync(request1);

        // Act - Turn 2
        var request2 = new ChatRequestDto(response1.ConversationId, "Also, password reset question.");
        var response2 = await _service.ProcessChatMessageAsync(request2);

        // Assert
        var savedSession = await _repo.GetByIdAsync(response1.ConversationId);
        Assert.That(savedSession, Is.Not.Null);
        Assert.That(savedSession!.Messages.Count, Is.EqualTo(4)); // User1, Assistant1, User2, Assistant2
    }
}
