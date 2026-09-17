using NUnit.Framework;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Services;
using ServiceDesk.Infrastructure.Services;

namespace PromptEval.Tests;

[TestFixture]
public class AdversarialPromptEvaluationTests
{
    private SecretRedactionService _redactionService = null!;

    [SetUp]
    public void SetUp()
    {
        _redactionService = new SecretRedactionService();
    }

    #region Secret Redaction Adversarial Tests

    [TestCase("My OpenAI key is sk-1234567890abcdef1234567890abcdef and VPN is down", "[REDACTED_SECRET]")]
    [TestCase("Database string: Password=SecretPass123;", "[REDACTED_SECRET]")]
    [TestCase("Password=SuperSecretPassword123;", "[REDACTED_SECRET]")]
    public void SecretRedaction_ShouldSanitizeSensitiveTokens(string inputRawText, string expectedRedactedPlaceholder)
    {
        var sanitized = _redactionService.RedactSecrets(inputRawText);

        Assert.That(sanitized, Is.Not.Null);
        Assert.That(sanitized, Does.Contain(expectedRedactedPlaceholder));
        Assert.That(sanitized, Does.Not.Contain("sk-1234567890abcdef1234567890abcdef"));
        Assert.That(sanitized, Does.Not.Contain("SecretPass123"));
    }

    #endregion

    #region Deterministic Priority Matrix Tests

    [TestCase(IncidentImpact.High, IncidentUrgency.High, IncidentPriority.Critical)]
    [TestCase(IncidentImpact.High, IncidentUrgency.Medium, IncidentPriority.High)]
    [TestCase(IncidentImpact.High, IncidentUrgency.Low, IncidentPriority.Medium)]
    [TestCase(IncidentImpact.Medium, IncidentUrgency.High, IncidentPriority.High)]
    [TestCase(IncidentImpact.Medium, IncidentUrgency.Medium, IncidentPriority.Medium)]
    [TestCase(IncidentImpact.Medium, IncidentUrgency.Low, IncidentPriority.Low)]
    [TestCase(IncidentImpact.Low, IncidentUrgency.High, IncidentPriority.Medium)]
    [TestCase(IncidentImpact.Low, IncidentUrgency.Medium, IncidentPriority.Low)]
    [TestCase(IncidentImpact.Low, IncidentUrgency.Low, IncidentPriority.Low)]
    public void PriorityCalculator_MatrixCombinations_ReturnsCorrectPriority(
        IncidentImpact impact,
        IncidentUrgency urgency,
        IncidentPriority expectedPriority)
    {
        var priority = DeterministicPriorityCalculator.Calculate(impact, urgency);

        Assert.That(priority, Is.EqualTo(expectedPriority));
    }

    #endregion

    #region Protected Write Boundary Invariants

    [Test]
    public void IncidentDraft_ApproveThenSubmit_SetsSubmittedStatus()
    {
        var draft = new IncidentDraft(Guid.NewGuid(), "VPN Outage", "User cannot connect to VPN", "Network", IncidentImpact.High, IncidentUrgency.High);

        Assert.That(draft.ApprovalStatus, Is.EqualTo(ApprovalStatus.Pending));
        Assert.That(draft.Status, Is.EqualTo(IncidentStatus.Draft));

        draft.ApproveByAuthenticatedUser();
        Assert.That(draft.ApprovalStatus, Is.EqualTo(ApprovalStatus.Approved));

        draft.MarkSubmitted("INC-2026-9999");
        Assert.That(draft.Status, Is.EqualTo(IncidentStatus.Submitted));
        Assert.That(draft.SubmittedIncidentId, Is.EqualTo("INC-2026-9999"));
    }

    [Test]
    public void IncidentDraft_MarkSubmittedWithoutApproval_ThrowsDomainException()
    {
        var draft = new IncidentDraft(Guid.NewGuid(), "VPN Outage", "User cannot connect to VPN", "Network", IncidentImpact.High, IncidentUrgency.High);

        Assert.Throws<ServiceDesk.Domain.Exceptions.DomainException>(() => draft.MarkSubmitted("INC-2026-9999"));
    }

    #endregion
}
