using NUnit.Framework;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace Application.Tests;

[TestFixture]
public class ApproveAndSubmitIncidentUseCaseTests
{
    private class TestUserContext : IUserContext
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = "user1";
        public string Email { get; set; } = "user1@company.com";
        public UserRole Role { get; set; } = UserRole.Employee;
        public bool IsAuthenticated { get; set; } = true;
    }

    private class MockIncidentGateway : IIncidentGateway
    {
        public bool CreateTicketCalled { get; private set; }
        public SubmitTicketRequestDto? LastRequest { get; private set; }

        public Task<TicketCreatedResponseDto> CreateTicketAsync(SubmitTicketRequestDto request, CancellationToken cancellationToken = default)
        {
            CreateTicketCalled = true;
            LastRequest = request;
            return Task.FromResult(new TicketCreatedResponseDto("INC-99999", "Created", DateTime.UtcNow, "Ticket created successfully."));
        }
    }

    [Test]
    public async Task ExecuteAsync_ShouldNotInvokeTicketApi_WhenUserRejects()
    {
        var userId = Guid.NewGuid();
        var userContext = new TestUserContext { UserId = userId, IsAuthenticated = true };
        var gateway = new MockIncidentGateway();
        var useCase = new ApproveAndSubmitIncidentUseCase(userContext, gateway);

        var draft = new IncidentDraft(userId, "Broken Key", "Spacebar broken", "Hardware", IncidentImpact.Low, IncidentUrgency.Low);
        var decision = new UserApprovalDecisionDto(draft.Id, Approved: false, UserNotes: "Don't submit yet");

        var result = await useCase.ExecuteAsync(draft, decision);

        Assert.That(result, Is.Null);
        Assert.That(gateway.CreateTicketCalled, Is.False);
        Assert.That(draft.ApprovalStatus, Is.EqualTo(ApprovalStatus.Rejected));
    }

    [Test]
    public async Task ExecuteAsync_ShouldInvokeTicketApi_WhenUserApproves()
    {
        var userId = Guid.NewGuid();
        var userContext = new TestUserContext { UserId = userId, IsAuthenticated = true };
        var gateway = new MockIncidentGateway();
        var useCase = new ApproveAndSubmitIncidentUseCase(userContext, gateway);

        var draft = new IncidentDraft(userId, "Screen Flickering", "Monitor flickering randomly", "Hardware", IncidentImpact.Medium, IncidentUrgency.Medium);
        var decision = new UserApprovalDecisionDto(draft.Id, Approved: true, UserNotes: "Approved by me");

        var result = await useCase.ExecuteAsync(draft, decision);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TicketId, Is.EqualTo("INC-99999"));
        Assert.That(gateway.CreateTicketCalled, Is.True);
        Assert.That(draft.ApprovalStatus, Is.EqualTo(ApprovalStatus.Approved));
        Assert.That(draft.SubmittedIncidentId, Is.EqualTo("INC-99999"));
        Assert.That(draft.Status, Is.EqualTo(IncidentStatus.Submitted));
    }
}
