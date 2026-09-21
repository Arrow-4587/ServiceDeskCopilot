using ServiceDesk.Application.DTOs.Dashboard;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace Application.Tests;

[TestFixture]
public class AnalystDashboardMetricsTests
{
    [Test]
    public void From_ShouldComputeAnalystCountsFromCurrentDatabaseState()
    {
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var tickets = new List<IncidentDraft>
        {
            new IncidentDraft(employeeId, "VPN disconnect", "VPN failed", "Network", IncidentImpact.High, IncidentUrgency.High),
            new IncidentDraft(managerId, "MFA reset", "MFA lockout", "Security", IncidentImpact.Medium, IncidentUrgency.High),
            new IncidentDraft(employeeId, "Laptop slow", "Performance issue", "Endpoints", IncidentImpact.Low, IncidentUrgency.Low),
            new IncidentDraft(otherUserId, "Unrelated request", "Should be ignored", "General", IncidentImpact.Low, IncidentUrgency.Low)
        };

        tickets[0].ApproveByAuthenticatedUser();
        tickets[1].RejectByAuthenticatedUser();

        var sessions = new List<ConversationSession>
        {
            new ConversationSession(employeeId),
            new ConversationSession(managerId),
            new ConversationSession(otherUserId),
            new ConversationSession(Guid.NewGuid())
        };

        sessions[0].AddMessage("User", "Need help with VPN");
        sessions[1].AddMessage("User", "MFA issue");
        sessions[2].AddMessage("User", "Unrelated");
        sessions[3].AddMessage("User", "Random");

        var summary = AnalystDashboardMetrics.From(
            tickets,
            sessions,
            new[] { employeeId, managerId },
            12);

        Assert.That(summary.TriageQueueCount, Is.EqualTo(3));
        Assert.That(summary.PendingApprovalCount, Is.EqualTo(1));
        Assert.That(summary.EndUserSessionCount, Is.EqualTo(2));
        Assert.That(summary.KnowledgeBaseCount, Is.EqualTo(12));
    }
}
