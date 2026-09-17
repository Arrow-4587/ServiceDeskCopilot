using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.DTOs.Status;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Adapters.Ai;
using ServiceDesk.Infrastructure.SemanticKernel.Agents;
using ServiceDesk.Infrastructure.SemanticKernel.Plugins;

namespace Integration.Tests.SemanticKernel;

[TestFixture]
public class SemanticKernelAgentWorkflowTests
{
    private class TestRetriever : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
        {
            var list = new List<SearchResultDto>
            {
                new SearchResultDto(
                    "VPN Policy",
                    "1.0",
                    "Troubleshooting",
                    2,
                    "Verify corporate credentials and client certificate before reconnecting to gateway.",
                    0.95,
                    true,
                    true,
                    "https://company.blob.core.windows.net/knowledge/vpn_policy.md"
                )
            };
            return Task.FromResult<IReadOnlyList<SearchResultDto>>(list);
        }
    }

    private class TestStatusReader : ISystemStatusReader
    {
        public Task<IReadOnlyList<ServiceStatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<ServiceStatusDto>
            {
                new ServiceStatusDto("GlobalProtect VPN", "Operational", "All gateways active.", DateTime.UtcNow),
                new ServiceStatusDto("Exchange Online", "DegradedPerformance", "Intermittent delays.", DateTime.UtcNow)
            };
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(list);
        }

        public Task<ServiceStatusDto?> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ServiceStatusDto?>(new ServiceStatusDto("GlobalProtect VPN", "Operational", "All gateways active.", DateTime.UtcNow));
        }
    }

    private class TestDraftRepository : IIncidentDraftRepository
    {
        public readonly List<IncidentDraft> Drafts = new();

        public Task<IncidentDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Drafts.FirstOrDefault(d => d.Id == id));

        public Task<IReadOnlyList<IncidentDraft>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IncidentDraft>>(Drafts.Where(d => d.UserId == userId).ToList());

        public Task<IReadOnlyList<IncidentDraft>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IncidentDraft>>(Drafts.ToList());

        public Task AddAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
        {
            Drafts.Add(draft);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class TestUserContext : IUserContext
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public string Username { get; } = "alex.smith";
        public string Email { get; } = "alex.smith@company.com";
        public UserRole Role { get; } = UserRole.Employee;
        public bool IsAuthenticated { get; set; } = true;
    }

    private class TestSecretRedactionService : ISecretRedactionService
    {
        public bool ContainsSecret(string text) => text.Contains("SecretKey999");
        public string RedactSecrets(string text) => text.Replace("SecretKey999", "[REDACTED_SECRET]");
    }

    private class TestAiTelemetry : IAiTelemetry
    {
        public List<string> Events { get; } = new();

        public void TrackRequestLatency(string operationName, TimeSpan duration, string correlationId) { }
        public void TrackTokenUsage(string modelName, int promptTokens, int completionTokens, string correlationId) { }
        public void TrackToolCall(string toolName, bool succeeded, string correlationId)
        {
            Events.Add($"TOOL:{toolName}");
        }
        public void TrackAgentStep(string agentName, string stepDescription, string correlationId)
        {
            Events.Add($"AGENT:{agentName}:{stepDescription}");
        }
    }

    private Kernel _kernel = null!;
    private KnowledgeSearchPlugin _knowledgePlugin = null!;
    private SystemStatusPlugin _statusPlugin = null!;
    private IncidentDraftPlugin _draftPlugin = null!;
    private SemanticKernelPlannerAgent _plannerAgent = null!;
    private SemanticKernelSpecialistAgent _specialistAgent = null!;
    private SemanticKernelReviewerAgent _reviewerAgent = null!;
    private SemanticKernelAgentWorkflowAdapter _adapter = null!;
    private TestDraftRepository _draftRepo = null!;
    private TestAiTelemetry _telemetry = null!;

    [SetUp]
    public void SetUp()
    {
        _kernel = Kernel.CreateBuilder().Build();
        _telemetry = new TestAiTelemetry();
        _draftRepo = new TestDraftRepository();
        var userContext = new TestUserContext();
        var redaction = new TestSecretRedactionService();

        var searchUseCase = new SearchKnowledgeUseCase(new TestRetriever());
        var statusUseCase = new GetSystemStatusUseCase(new TestStatusReader());
        var draftUseCase = new CreateIncidentDraftUseCase(userContext, _draftRepo);

        _knowledgePlugin = new KnowledgeSearchPlugin(searchUseCase, NullLogger<KnowledgeSearchPlugin>.Instance);
        _statusPlugin = new SystemStatusPlugin(statusUseCase, NullLogger<SystemStatusPlugin>.Instance);
        _draftPlugin = new IncidentDraftPlugin(draftUseCase, userContext, NullLogger<IncidentDraftPlugin>.Instance);

        _plannerAgent = new SemanticKernelPlannerAgent(_kernel, NullLogger<SemanticKernelPlannerAgent>.Instance);
        _specialistAgent = new SemanticKernelSpecialistAgent(_kernel, NullLogger<SemanticKernelSpecialistAgent>.Instance);
        _reviewerAgent = new SemanticKernelReviewerAgent(_kernel, redaction, draftUseCase, userContext, NullLogger<SemanticKernelReviewerAgent>.Instance);

        _adapter = new SemanticKernelAgentWorkflowAdapter(
            _plannerAgent,
            _specialistAgent,
            _reviewerAgent,
            _knowledgePlugin,
            _statusPlugin,
            _telemetry,
            NullLogger<SemanticKernelAgentWorkflowAdapter>.Instance
        );
    }

    [Test]
    public async Task KnowledgeSearchPlugin_ReturnsCloudBlobPathsAndPolicyDetails()
    {
        var resultJson = await _knowledgePlugin.SearchPolicyDocumentsAsync("VPN");
        Assert.That(resultJson, Does.Contain("VPN Policy"));
        Assert.That(resultJson, Does.Contain("https://company.blob.core.windows.net/knowledge/vpn_policy.md"));
    }

    [Test]
    public async Task SystemStatusPlugin_ReturnsLiveServiceHealth()
    {
        var resultJson = await _statusPlugin.CheckServiceHealthAsync("VPN");
        Assert.That(resultJson, Does.Contain("GlobalProtect VPN"));
        Assert.That(resultJson, Does.Contain("Operational"));
    }

    [Test]
    public async Task Workflow_PolicyInquiry_InvokesPlannerAndSpecialistWithCloudCitations()
    {
        var request = new ChatRequestDto(Guid.NewGuid(), "How do I troubleshoot VPN?");
        var response = await _adapter.ExecuteWorkflowAsync(request);

        Assert.That(response, Is.Not.Null);
        Assert.That(response.Answer, Does.Contain("VPN Policy"));
        Assert.That(response.Citations.Count, Is.GreaterThan(0));
        Assert.That(response.Citations[0].BlobPath, Is.EqualTo("https://company.blob.core.windows.net/knowledge/vpn_policy.md"));
        Assert.That(_telemetry.Events.Any(e => e.Contains("SemanticKernelPlannerAgent")), Is.True);
        Assert.That(_telemetry.Events.Any(e => e.Contains("SemanticKernelSpecialistAgent")), Is.True);
    }

    [Test]
    public async Task Workflow_TicketEscalation_InvokesReviewerAndRedactsSecrets()
    {
        var request = new ChatRequestDto(Guid.NewGuid(), "Please raise ticket for display issue SecretKey999");
        var response = await _adapter.ExecuteWorkflowAsync(request);

        Assert.That(response.SuggestedIncidentDraft, Is.True);
        Assert.That(response.SuggestedDraftId, Is.Not.Null);
        Assert.That(response.SuggestedTitle, Does.Not.Contain("SecretKey999"));
        Assert.That(response.SuggestedDescription, Does.Not.Contain("SecretKey999"));
        Assert.That(_telemetry.Events.Any(e => e.Contains("SemanticKernelReviewerAgent")), Is.True);
        Assert.That(_draftRepo.Drafts.Count, Is.EqualTo(1));
    }
}
