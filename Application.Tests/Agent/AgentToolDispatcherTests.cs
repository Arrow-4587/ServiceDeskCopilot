using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.DTOs.Status;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Enums;

namespace Application.Tests.Agent;

[TestFixture]
public class AgentToolDispatcherTests
{
    private class FakeRetriever : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<SearchResultDto>> SearchKnowledgeAsync(SearchQueryDto query, CancellationToken cancellationToken = default)
        {
            var list = new List<SearchResultDto>
            {
                new SearchResultDto("VPN Guide", "1.0", "General", 1, "VPN Content", 2.0, true, true, "data/knowledge/vpn.md")
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
                new ServiceStatusDto("VPN", "Operational", "VPN OK", DateTime.UtcNow)
            };
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(list);
        }

        public Task<ServiceStatusDto?> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ServiceStatusDto?>(new ServiceStatusDto(serviceName, "Operational", "OK", DateTime.UtcNow));
        }
    }

    private class FakeUserContext : IUserContext
    {
        public Guid UserId => Guid.NewGuid();
        public string Username => "jdoe";
        public string Email => "user@company.com";
        public UserRole Role => UserRole.Employee;
        public bool IsAuthenticated => true;
    }

    private AgentToolDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp()
    {
        var retriever = new FakeRetriever();
        var searchUseCase = new SearchKnowledgeUseCase(retriever);

        var statusReader = new FakeStatusReader();
        var statusUseCase = new GetSystemStatusUseCase(statusReader);

        var userContext = new FakeUserContext();
        var draftUseCase = new CreateIncidentDraftUseCase(userContext);

        var logger = NullLogger<AgentToolDispatcher>.Instance;

        _dispatcher = new AgentToolDispatcher(searchUseCase, statusUseCase, draftUseCase, logger);
    }

    [Test]
    public void GetAvailableTools_ReturnsThreeToolsWithMetadata()
    {
        // Act
        var tools = _dispatcher.GetAvailableTools();

        // Assert
        Assert.That(tools, Is.Not.Null);
        Assert.That(tools.Count, Is.EqualTo(3));
        Assert.That(tools.Any(t => t.Name == "KnowledgeSearch"), Is.True);
        Assert.That(tools.Any(t => t.Name == "SystemStatus"), Is.True);
        Assert.That(tools.Any(t => t.Name == "CreateIncidentDraft"), Is.True);
    }

    [Test]
    public async Task ExecuteToolAsync_KnowledgeSearch_ReturnsSuccessfulResult()
    {
        // Arrange
        var args = new Dictionary<string, string> { { "queryText", "VPN connection" } };

        // Act
        var result = await _dispatcher.ExecuteToolAsync("KnowledgeSearch", args);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Contains.Substring("VPN Guide"));
    }

    [Test]
    public async Task ExecuteToolAsync_SystemStatus_ReturnsSuccessfulResult()
    {
        // Arrange
        var args = new Dictionary<string, string> { { "serviceName", "VPN" } };

        // Act
        var result = await _dispatcher.ExecuteToolAsync("SystemStatus", args);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Contains.Substring("Operational"));
    }

    [Test]
    public async Task ExecuteToolAsync_CreateIncidentDraft_ReturnsSuccessfulResult()
    {
        // Arrange
        var args = new Dictionary<string, string>
        {
            { "title", "Cannot connect to VPN" },
            { "description", "Cisco AnyConnect gives error 401." },
            { "impact", "Medium" },
            { "urgency", "High" }
        };

        // Act
        var result = await _dispatcher.ExecuteToolAsync("CreateIncidentDraft", args);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Contains.Substring("Cannot connect to VPN"));
    }

    [Test]
    public async Task ExecuteToolAsync_UnrecognizedTool_ReturnsErrorResult()
    {
        // Arrange
        var args = new Dictionary<string, string>();

        // Act
        var result = await _dispatcher.ExecuteToolAsync("NonExistentTool", args);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("Unrecognized tool"));
    }
}
