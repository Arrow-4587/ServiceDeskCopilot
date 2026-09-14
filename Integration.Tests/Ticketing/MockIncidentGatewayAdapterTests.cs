using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Adapters.Ticketing;

namespace Integration.Tests.Ticketing;

[TestFixture]
public class MockIncidentGatewayAdapterTests
{
    [Test]
    public async Task CreateTicketAsync_ReturnsFormattedTicketIdAndSubmittedStatus()
    {
        // Arrange
        var logger = NullLogger<MockIncidentGatewayAdapter>.Instance;
        var gateway = new MockIncidentGatewayAdapter(logger);

        var request = new SubmitTicketRequestDto(
            DraftId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: "VPN Connection Timeout",
            Description: "Cisco AnyConnect authentication fails consistently.",
            Category: "Network",
            Priority: IncidentPriority.High,
            SystemStatusEvidence: "VPN Concentrator active"
        );

        // Act
        var result = await gateway.CreateTicketAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TicketId, Contains.Substring("INC-2026-"));
        Assert.That(result.Status, Is.EqualTo("Submitted"));
        Assert.That(result.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }
}
