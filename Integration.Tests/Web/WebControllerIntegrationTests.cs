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
using ServiceDesk.Web.Controllers;

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
        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            NullLogger<ChatController>.Instance);

        var result = await controller.Index(null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task ChatController_SendMessage_ValidRequest_ReturnsJsonResult()
    {
        var chatService = new FakeChatConversationService();
        var controller = new ChatController(
            chatService,
            _conversationRepository,
            _userContext,
            NullLogger<ChatController>.Instance);

        var request = new ChatRequestDto(Guid.Empty, "How do I setup VPN?");
        var result = await controller.SendMessage(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task IncidentController_Index_ReturnsViewResultWithDrafts()
    {
        var draft = new IncidentDraft(_userContext.UserId, "VPN Issue", "Cannot connect to corporate VPN", "Network", IncidentImpact.Medium, IncidentUrgency.Medium);
        await _draftRepository.AddAsync(draft);

        var reviewer = new IncidentReviewerService(new SecretRedactionService(), NullLogger<IncidentReviewerService>.Instance);
        var gateway = new MockIncidentGatewayAdapter(NullLogger<MockIncidentGatewayAdapter>.Instance);
        var useCase = new ApproveAndSubmitIncidentUseCase(_userContext, gateway);

        var controller = new IncidentController(
            _draftRepository,
            reviewer,
            useCase,
            _userContext,
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

        var controller = new IncidentController(
            _draftRepository,
            reviewer,
            useCase,
            _userContext,
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
                convId,
                "Fake answer from copilot",
                Array.Empty<CitationDto>(),
                RequiresClarification: false,
                SuggestedIncidentDraft: false,
                SuggestedTitle: null,
                SuggestedDescription: null
            ));
        }
    }

    private class TempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
