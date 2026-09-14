using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace Application.Tests.Agent;

[TestFixture]
public class IncidentReviewerServiceTests
{
    private class FakeSecretRedactionService : ISecretRedactionService
    {
        public bool ContainsSecret(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return input.Contains("sk-") || input.Contains("password=");
        }

        public string RedactSecrets(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string result = input;
            if (result.Contains("sk-123456789012345678901234567"))
                result = result.Replace("sk-123456789012345678901234567", "[REDACTED_SECRET]");
            if (result.Contains("password=Secret123!"))
                result = result.Replace("password=Secret123!", "[REDACTED_SECRET]");
            return result;
        }
    }

    private IncidentReviewerService _reviewerService = null!;

    [SetUp]
    public void SetUp()
    {
        var redactionService = new FakeSecretRedactionService();
        var logger = NullLogger<IncidentReviewerService>.Instance;
        _reviewerService = new IncidentReviewerService(redactionService, logger);
    }

    [Test]
    public async Task ReviewDraftAsync_ValidDraft_ReturnsIsValidTrueAndCalculatesPriority()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var draft = new IncidentDraft(
            userId: userId,
            title: "VPN AnyConnect Authentication Timeout",
            description: "Whenever I try to log into Cisco AnyConnect, the client times out at 99%.",
            category: "Network",
            impact: IncidentImpact.High,
            urgency: IncidentUrgency.High
        );

        // Act
        var result = await _reviewerService.ReviewDraftAsync(draft);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PrivacyCheckPassed, Is.True);
        Assert.That(result.CalculatedPriority, Is.EqualTo(IncidentPriority.Critical));
    }

    [Test]
    public async Task ReviewDraftAsync_IncompleteDraft_ReturnsIsValidFalseWithValidationFeedback()
    {
        // Arrange
        var dto = new CreateIncidentDraftDto(
            Title: "VPN", // Too short (< 5 chars)
            Description: "Broken", // Too short (< 15 chars)
            Category: "General IT",
            Impact: IncidentImpact.Low,
            Urgency: IncidentUrgency.Low
        );

        // Act
        var result = await _reviewerService.ReviewDraftDtoAsync(dto);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ValidationFeedback.Any(f => f.Contains("Title")), Is.True);
        Assert.That(result.ValidationFeedback.Any(f => f.Contains("Description")), Is.True);
    }

    [Test]
    public async Task ReviewDraftAsync_DraftWithEmbeddedSecret_RedactsSecretsAndFlagsPrivacyCheck()
    {
        // Arrange
        var dto = new CreateIncidentDraftDto(
            Title: "Issue with api-key sk-123456789012345678901234567",
            Description: "My connection string is Server=db.company.com;Database=prod;password=Secret123! and it failed.",
            Category: "Database",
            Impact: IncidentImpact.Medium,
            Urgency: IncidentUrgency.Medium
        );

        // Act
        var result = await _reviewerService.ReviewDraftDtoAsync(dto);

        // Assert
        Assert.That(result.PrivacyCheckPassed, Is.False);
        Assert.That(result.RedactedTitle, Contains.Substring("[REDACTED_SECRET]"));
        Assert.That(result.RedactedDescription, Contains.Substring("[REDACTED_SECRET]"));
        Assert.That(result.ValidationFeedback.Any(f => f.Contains("Privacy compliance warning")), Is.True);
    }
}
