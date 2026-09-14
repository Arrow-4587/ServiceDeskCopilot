using NUnit.Framework;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace Domain.Tests;

[TestFixture]
public class IncidentDraftTests
{
    [Test]
    public void CreateDraft_ShouldComputePriority_AndSetStatusToPendingAndDraft()
    {
        var userId = Guid.NewGuid();
        var draft = new IncidentDraft(userId, "VPN Connectivity Issue", "Cannot connect to Cisco AnyConnect VPN", "Network", IncidentImpact.High, IncidentUrgency.High);

        Assert.That(draft.UserId, Is.EqualTo(userId));
        Assert.That(draft.ComputedPriority, Is.EqualTo(IncidentPriority.Critical));
        Assert.That(draft.ApprovalStatus, Is.EqualTo(ApprovalStatus.Pending));
        Assert.That(draft.Status, Is.EqualTo(IncidentStatus.Draft));
    }

    [Test]
    public void ApproveByAuthenticatedUser_ShouldChangeApprovalStatusToApproved()
    {
        var draft = new IncidentDraft(Guid.NewGuid(), "Printer Broken", "Paper jam in 3rd floor printer", "Hardware", IncidentImpact.Low, IncidentUrgency.Low);

        draft.ApproveByAuthenticatedUser();

        Assert.That(draft.ApprovalStatus, Is.EqualTo(ApprovalStatus.Approved));
        Assert.That(draft.ApprovedAt, Is.Not.Null);
    }

    [Test]
    public void MarkSubmitted_ShouldThrowDomainException_IfDraftNotApproved()
    {
        var draft = new IncidentDraft(Guid.NewGuid(), "Laptop Slowness", "Laptop takes 10 minutes to boot", "Hardware", IncidentImpact.Medium, IncidentUrgency.Low);

        Assert.Throws<DomainException>(() => draft.MarkSubmitted("INC-10001"));
    }

    [Test]
    public void MarkSubmitted_ShouldSucceed_WhenDraftIsApproved()
    {
        var draft = new IncidentDraft(Guid.NewGuid(), "Email Sync Fail", "Outlook is stuck updating folders", "Software", IncidentImpact.Medium, IncidentUrgency.Medium);

        draft.ApproveByAuthenticatedUser();
        draft.MarkSubmitted("INC-10002");

        Assert.That(draft.SubmittedIncidentId, Is.EqualTo("INC-10002"));
        Assert.That(draft.Status, Is.EqualTo(IncidentStatus.Submitted));
    }
}
