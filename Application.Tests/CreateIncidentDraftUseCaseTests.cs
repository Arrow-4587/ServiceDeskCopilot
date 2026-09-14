using NUnit.Framework;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Enums;

namespace Application.Tests;

[TestFixture]
public class CreateIncidentDraftUseCaseTests
{
    private class TestUserContext : IUserContext
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = "testuser";
        public string Email { get; set; } = "testuser@company.com";
        public UserRole Role { get; set; } = UserRole.Employee;
        public bool IsAuthenticated { get; set; } = true;
    }

    [Test]
    public void Execute_ShouldThrowApplicationAuthorizationException_WhenUserNotAuthenticated()
    {
        var userContext = new TestUserContext { IsAuthenticated = false };
        var useCase = new CreateIncidentDraftUseCase(userContext);
        var dto = new CreateIncidentDraftDto("Title", "Desc", "IT", IncidentImpact.High, IncidentUrgency.High);

        Assert.Throws<ApplicationAuthorizationException>(() => useCase.Execute(dto));
    }

    [Test]
    public void Execute_ShouldThrowValidationException_WhenTitleIsEmpty()
    {
        var userContext = new TestUserContext { IsAuthenticated = true };
        var useCase = new CreateIncidentDraftUseCase(userContext);
        var dto = new CreateIncidentDraftDto("", "Desc", "IT", IncidentImpact.High, IncidentUrgency.High);

        Assert.Throws<ValidationException>(() => useCase.Execute(dto));
    }

    [Test]
    public void Execute_ShouldCreateDraft_WithDeterministicPriority()
    {
        var userContext = new TestUserContext { IsAuthenticated = true };
        var useCase = new CreateIncidentDraftUseCase(userContext);
        var dto = new CreateIncidentDraftDto("VPN Disconnected", "Cannot connect to VPN", "Network", IncidentImpact.High, IncidentUrgency.High);

        var result = useCase.Execute(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Title, Is.EqualTo("VPN Disconnected"));
        Assert.That(result.ComputedPriority, Is.EqualTo(IncidentPriority.Critical));
        Assert.That(result.ApprovalStatus, Is.EqualTo(ApprovalStatus.Pending));
    }
}
