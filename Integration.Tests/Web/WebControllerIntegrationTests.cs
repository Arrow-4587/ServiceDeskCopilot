using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Adapters.Status;
using ServiceDesk.Infrastructure.Adapters.Ticketing;
using ServiceDesk.Infrastructure.Services;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Web.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests.Web;

[TestFixture]
public class WebControllerIntegrationTests
{
    private FakeConversationRepository _conversationRepository = null!;
    private FakeIncidentDraftRepository _draftRepository = null!;
    private FakeUserContext _userContext = null!;

    [SetUp]
    public void SetUp()
    {
        _conversationRepository = new FakeConversationRepository();
        _draftRepository = new FakeIncidentDraftRepository();
        _userContext = new FakeUserContext
        {
            UserId = Guid.NewGuid(),
            Username = "test.user",
            Email = "test@company.com",
            Role = UserRole.Employee,
            IsAuthenticated = true
        };
    }

    [Test]
    public async Task ChatController_Index_ReturnsViewResult()
    {
        var chatService = new FakeChatConversationService();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            dbContext,
            NullLogger<ChatController>.Instance);

        var result = await controller.Index(null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task ChatController_SendMessage_ValidRequest_ReturnsJsonResult()
    {
        var chatService = new FakeChatConversationService();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            dbContext,
            NullLogger<ChatController>.Instance);

        var request = new ChatRequestDto(Guid.Empty, "How do I setup VPN?");
        var result = await controller.SendMessage(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task ChatController_Index_Employee_AccessingOtherUserConversation_ReturnsForbidResult()
    {
        var otherUserId = Guid.NewGuid();
        var session = new ConversationSession(otherUserId);
        await _conversationRepository.AddAsync(session);

        var chatService = new FakeChatConversationService();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            dbContext,
            NullLogger<ChatController>.Instance);

        var result = await controller.Index(session.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task ChatController_GetConversationApi_OtherUserEmployee_ReturnsForbidResult()
    {
        var otherUserId = Guid.NewGuid();
        var session = new ConversationSession(otherUserId);
        await _conversationRepository.AddAsync(session);

        var chatService = new FakeChatConversationService();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            dbContext,
            NullLogger<ChatController>.Instance);

        var result = await controller.GetConversationApi(session.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task ChatController_GetConversationApi_ValidUser_ReturnsJsonResultWithMessages()
    {
        var session = new ConversationSession(_userContext.UserId);
        session.AddUserMessage("How do I connect to VPN?");
        session.AddAssistantMessage("Use Cisco AnyConnect with your corporate credentials.");
        await _conversationRepository.AddAsync(session);

        var chatService = new FakeChatConversationService();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            dbContext,
            NullLogger<ChatController>.Instance);

        var result = await controller.GetConversationApi(session.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        var detail = jsonResult.Value as ConversationDetailDto;
        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.Messages.Count, Is.EqualTo(2));
        Assert.That(detail.Messages[0].Content, Is.EqualTo("How do I connect to VPN?"));
    }

    [Test]
    public async Task ChatController_HistoryApi_SearchFilter_ReturnsMatchingConversations()
    {
        var session1 = new ConversationSession(_userContext.UserId);
        session1.AddUserMessage("VPN connection dropping");
        await _conversationRepository.AddAsync(session1);

        var session2 = new ConversationSession(_userContext.UserId);
        session2.AddUserMessage("Printer paper jam");
        await _conversationRepository.AddAsync(session2);

        var chatService = new FakeChatConversationService();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            dbContext,
            NullLogger<ChatController>.Instance);

        var result = await controller.HistoryApi(1, 10, "VPN", "all", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        var paged = jsonResult.Value as PaginatedConversationHistoryDto;
        Assert.That(paged, Is.Not.Null);
        Assert.That(paged!.TotalCount, Is.EqualTo(1));
        Assert.That(paged.Items[0].Id, Is.EqualTo(session1.Id));
    }

    [Test]
    public async Task IncidentController_Index_ReturnsViewResultWithDrafts()
    {
        var draft = new IncidentDraft(_userContext.UserId, "VPN Issue", "Cannot connect to corporate VPN", "Network", IncidentImpact.Medium, IncidentUrgency.Medium);
        await _draftRepository.AddAsync(draft);

        var reviewer = new IncidentReviewerService(new SecretRedactionService(), NullLogger<IncidentReviewerService>.Instance);
        var gateway = new MockIncidentGatewayAdapter(NullLogger<MockIncidentGatewayAdapter>.Instance);
        var useCase = new ApproveAndSubmitIncidentUseCase(_userContext, gateway);
        var knowledgeService = new FakeKnowledgeBaseService();

        var controller = new IncidentController(
            _draftRepository,
            reviewer,
            useCase,
            _userContext,
            knowledgeService,
            NullLogger<IncidentController>.Instance);

        var result = await controller.Index(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(viewResult.Model, Is.InstanceOf<IReadOnlyList<IncidentDraft>>());
        var drafts = (IReadOnlyList<IncidentDraft>)viewResult.Model!;
        Assert.That(drafts.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task IncidentController_ApproveOrReject_Approve_ExecutesUseCaseAndRedirects()
    {
        var draft = new IncidentDraft(_userContext.UserId, "Email Down", "Exchange server not responding", "Email", IncidentImpact.High, IncidentUrgency.High);
        await _draftRepository.AddAsync(draft);

        var reviewer = new IncidentReviewerService(new SecretRedactionService(), NullLogger<IncidentReviewerService>.Instance);
        var gateway = new MockIncidentGatewayAdapter(NullLogger<MockIncidentGatewayAdapter>.Instance);
        var useCase = new ApproveAndSubmitIncidentUseCase(_userContext, gateway);
        var knowledgeService = new FakeKnowledgeBaseService();

        var controller = new IncidentController(
            _draftRepository,
            reviewer,
            useCase,
            _userContext,
            knowledgeService,
            NullLogger<IncidentController>.Instance);

        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TempDataProvider());
        controller.TempData = tempData;

        var result = await controller.ApproveOrReject(draft.Id, approved: true, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var updatedDraft = await _draftRepository.GetByIdAsync(draft.Id);
        Assert.That(updatedDraft!.ApprovalStatus, Is.EqualTo(ApprovalStatus.Approved));
        Assert.That(updatedDraft.Status, Is.EqualTo(IncidentStatus.Submitted));
        Assert.That(updatedDraft.SubmittedIncidentId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task StatusController_Index_ReturnsViewResultWithServiceStatuses()
    {
        var statusReader = new FakeSystemStatusReader();
        var controller = new StatusController(statusReader, NullLogger<StatusController>.Instance);

        var result = await controller.Index(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(viewResult.Model, Is.InstanceOf<IReadOnlyList<ServiceDesk.Application.DTOs.Status.ServiceStatusDto>>());
        var statuses = (IReadOnlyList<ServiceDesk.Application.DTOs.Status.ServiceStatusDto>)viewResult.Model!;
        Assert.That(statuses.Count, Is.GreaterThan(0));
    }

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
            if (!string.IsNullOrWhiteSpace(timeFilter))
            {
                var now = DateTime.UtcNow;
                var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                switch (timeFilter.ToLower().Trim())
                {
                    case "today":
                        query = query.Where(s => s.StartedAt >= todayStart);
                        break;
                    case "yesterday":
                        var yesterdayStart = todayStart.AddDays(-1);
                        query = query.Where(s => s.StartedAt >= yesterdayStart && s.StartedAt < todayStart);
                        break;
                    case "week":
                    case "7days":
                        var weekStart = todayStart.AddDays(-7);
                        query = query.Where(s => s.StartedAt >= weekStart);
                        break;
                    case "older":
                        var olderStart = todayStart.AddDays(-7);
                        query = query.Where(s => s.StartedAt < olderStart);
                        break;
                }
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

    private class FakeIncidentDraftRepository : IIncidentDraftRepository
    {
        private readonly Dictionary<Guid, IncidentDraft> _store = new();
        public Task<IncidentDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(id, out var draft);
            return Task.FromResult(draft);
        }
        public Task AddAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
        {
            _store[draft.Id] = draft;
            return Task.CompletedTask;
        }
        public Task UpdateAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
        {
            _store[draft.Id] = draft;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<IncidentDraft>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var list = _store.Values.Where(d => d.UserId == userId).ToList();
            return Task.FromResult<IReadOnlyList<IncidentDraft>>(list);
        }
        public Task<IReadOnlyList<IncidentDraft>> GetPendingDraftsAsync(CancellationToken cancellationToken = default)
        {
            var list = _store.Values.Where(d => d.ApprovalStatus == ApprovalStatus.Pending).ToList();
            return Task.FromResult<IReadOnlyList<IncidentDraft>>(list);
        }
        public Task<IReadOnlyList<IncidentDraft>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var list = _store.Values.ToList();
            return Task.FromResult<IReadOnlyList<IncidentDraft>>(list);
        }
    }

    private class FakeUserContext : IUserContext
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsAuthenticated { get; set; }
    }

    private class FakeChatConversationService : IChatConversationService
    {
        public Task<ChatResponseDto> ProcessChatMessageAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var convId = request.ConversationId != Guid.Empty ? request.ConversationId : Guid.NewGuid();
            return Task.FromResult(new ChatResponseDto(
                request.ConversationId ?? Guid.NewGuid(),
                "Fake answer from copilot",
                Array.Empty<CitationDto>(),
                RequiresClarification: false,
                SuggestedIncidentDraft: false,
                SuggestedTitle: null,
                SuggestedDescription: null
            ));
        }
    }

    private class FakeKnowledgeBaseService : IKnowledgeBaseService
    {
        public Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedDocumentsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(Array.Empty<KnowledgeDocumentDto>());
        }

        public Task<KnowledgeDocumentDetailDto?> GetDocumentDetailAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<KnowledgeDocumentDetailDto?>(null);
        }

        public void InvalidateCache()
        {
        }
    }

    private class TempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
