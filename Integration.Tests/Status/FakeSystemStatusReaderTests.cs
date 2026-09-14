using NUnit.Framework;
using ServiceDesk.Infrastructure.Adapters.Status;

namespace Integration.Tests.Status;

[TestFixture]
public class FakeSystemStatusReaderTests
{
    [Test]
    public async Task GetAllStatusesAsync_ReturnsAllPreconfiguredSystemStatuses()
    {
        // Arrange
        var statusReader = new FakeSystemStatusReader();

        // Act
        var statuses = await statusReader.GetAllStatusesAsync();

        // Assert
        Assert.That(statuses, Is.Not.Null);
        Assert.That(statuses.Count, Is.GreaterThanOrEqualTo(5));
        Assert.That(statuses.Any(s => s.ServiceName.Contains("VPN")), Is.True);
        Assert.That(statuses.Any(s => s.ServiceName.Contains("Outlook")), Is.True);
        Assert.That(statuses.Any(s => s.ServiceName.Contains("Teams")), Is.True);
    }

    [Test]
    public async Task GetServiceStatusAsync_ReturnsMatchingStatus_ForExactAndFuzzyQueries()
    {
        // Arrange
        var statusReader = new FakeSystemStatusReader();

        // Act
        var vpnStatus = await statusReader.GetServiceStatusAsync("vpn");
        var mailStatus = await statusReader.GetServiceStatusAsync("email");
        var unknownStatus = await statusReader.GetServiceStatusAsync("non-existent-system-xyz");

        // Assert
        Assert.That(vpnStatus, Is.Not.Null);
        Assert.That(vpnStatus!.Status, Is.EqualTo("Operational"));

        Assert.That(mailStatus, Is.Not.Null);
        Assert.That(mailStatus!.ServiceName.Contains("Outlook"), Is.True);

        Assert.That(unknownStatus, Is.Null);
    }
}
