using NUnit.Framework;
using ServiceDesk.Domain.Exceptions;
using ServiceDesk.Domain.ValueObjects;

namespace Domain.Tests;

[TestFixture]
public class KnowledgeMetadataTests
{
    [Test]
    public void IsValidForGrounding_ShouldBeTrue_OnlyWhenApprovedAndActive()
    {
        var metadata = new KnowledgeMetadata("VPN Policy", "1.0", "Section 2", 3, approved: true, active: true);

        Assert.That(metadata.IsValidForGrounding, Is.True);
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public void IsValidForGrounding_ShouldBeFalse_WhenNotApprovedOrNotActive(bool approved, bool active)
    {
        var metadata = new KnowledgeMetadata("VPN Policy", "1.0", "Section 2", 3, approved: approved, active: active);

        Assert.That(metadata.IsValidForGrounding, Is.False);
    }

    [Test]
    public void Constructor_ShouldThrowDomainException_WhenDocumentNameIsEmpty()
    {
        Assert.Throws<DomainException>(() => new KnowledgeMetadata("", "1.0", "Sec 1", 1, true, true));
    }
}
