using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.DTOs.Status;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace Application.Tests.Agent;

[TestFixture]
public class AgentWorkflowCoordinatorTests
{
    private class TestChatModel : IChatModel
    {
        public Task<ChatResponseDto> GenerateCompletionAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            string answer = "According to VPN Guide: Verify credentials and certificate before reconnecting.";
            return Task.FromResult(new ChatResponseDto(
                ConversationId: request.ConversationId ?? Guid.NewGuid(),
                Answer: answer,
                Citations: Array.Empty<CitationDto>()
            ));
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(ChatRequestDto request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "Test completion response";
            await Task.CompletedTask;
        }
    }

    private class FakeRetriever : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
        {
            var list = new List<SearchResultDto>
            {
                new SearchResultDto("VPN Guide", "1.0", "Troubleshooting", 1, "Verify credentials and certificate before reconnecting.", 0.9, true, true, "/vpn.md")
            };
            return Task.FromResult<IReadOnlyList<SearchResultDto>>(list);
        }
    }

    private class FakeStatusReader : ISystemStatusReader
    {
        public Task<IReadOnlyList<ServiceStatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<ServiceStatusDto>
            {
                new ServiceStatusDto("GlobalProtect VPN", "Operational", "All gateways active", DateTime.UtcNow)
            };
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(list);
        }

        public Task<ServiceStatusDto?> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ServiceStatusDto?>(new ServiceStatusDto(serviceName, "Operational", "Active", DateTime.UtcNow));
        }
    }

    private class FakeDraftRepository : IIncidentDraftRepository
    {
        private readonly List<IncidentDraft> _drafts = new();

        public Task<IncidentDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_drafts.FirstOrDefault(d => d.Id == id));

        public Task<IReadOnlyList<IncidentDraft>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IncidentDraft>>(_drafts.Where(d => d.UserId == userId).ToList());

        public Task<IReadOnlyList<IncidentDraft>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IncidentDraft>>(_drafts.ToList());

        public Task AddAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
        {
            _drafts.Add(draft);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class FakeUserContext : IUserContext
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public string Username { get; } = "john.doe";
        public string Email { get; } = "john.doe@enterprise.com";
        public UserRole Role { get; } = UserRole.Employee;
        public bool IsAuthenticated { get; set; } = true;
    }

    private class FakeTelemetry : IAiTelemetry
    {
        public List<string> Steps { get; } = new();

        public void TrackRequestLatency(string operationName, TimeSpan duration, string correlationId) { }
        public void TrackTokenUsage(string modelName, int promptTokens, int completionTokens, string correlationId) { }
        public void TrackToolCall(string toolName, bool succeeded, string correlationId) { }
        public void TrackAgentStep(string agentName, string stepDescription, string correlationId)
        {
            Steps.Add($"{agentName}:{stepDescription}");
        }
    }

    private class FakeSecretRedactionService : ISecretRedactionService
    {
        public bool ContainsSecret(string text) => text.Contains("password123");
        public string RedactSecrets(string text) => text.Replace("password123", "[REDACTED_SECRET]");
    }

    private AgentWorkflowCoordinator _coordinator = null!;
    private FakeTelemetry _telemetry = null!;
    private FakeDraftRepository _draftRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _telemetry = new FakeTelemetry();
        _draftRepo = new FakeDraftRepository();
        var userContext = new FakeUserContext();

        var planner = new PlannerService(NullLogger<PlannerService>.Instance);
        var mockChatModel = new TestChatModel();
        var specialist = new SupportSpecialistService(mockChatModel, NullLogger<SupportSpecialistService>.Instance);
        var reviewer = new IncidentReviewerService(new FakeSecretRedactionService(), NullLogger<IncidentReviewerService>.Instance);

        var searchUseCase = new SearchKnowledgeUseCase(new FakeRetriever());
        var statusUseCase = new GetSystemStatusUseCase(new FakeStatusReader());
        var draftUseCase = new CreateIncidentDraftUseCase(userContext, _draftRepo);

        _coordinator = new AgentWorkflowCoordinator(
            planner,
            specialist,
            reviewer,
            searchUseCase,
            statusUseCase,
            draftUseCase,
            userContext,
            _telemetry,
            NullLogger<AgentWorkflowCoordinator>.Instance,
            _draftRepo
        );
    }

    [Test]
    public async Task ExecuteWorkflowAsync_WithPolicyQuestion_CoordinatesPlannerAndSpecialist()
    {
        var request = new ChatRequestDto(Guid.NewGuid(), "How do I fix VPN connection error?");
        var result = await _coordinator.ExecuteWorkflowAsync(request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Answer, Does.Contain("VPN Guide"));
        Assert.That(result.Citations.Count, Is.GreaterThan(0));
        Assert.That(result.Citations[0].DocumentName, Is.EqualTo("VPN Guide"));
        Assert.That(_telemetry.Steps.Any(s => s.StartsWith("PlannerAgent:")), Is.True);
        Assert.That(_telemetry.Steps.Any(s => s.StartsWith("SupportSpecialistAgent:")), Is.True);
    }

    [Test]
    public async Task ExecuteWorkflowAsync_WithTicketRequest_InvokesReviewerAndPersistsDraft()
    {
        var request = new ChatRequestDto(Guid.NewGuid(), "Please raise ticket for password123 leaked on display");
        var result = await _coordinator.ExecuteWorkflowAsync(request);

        Assert.That(result.SuggestedIncidentDraft, Is.True);
        Assert.That(result.SuggestedDraftId, Is.Not.Null);
        Assert.That(result.SuggestedTitle, Does.Not.Contain("password123"));
        Assert.That(_telemetry.Steps.Any(s => s.StartsWith("ReviewerAgent:")), Is.True);
    }
}
