using NUnit.Framework;
using ServiceDesk.Infrastructure.Services;

namespace Integration.Tests.Ai;

[TestFixture]
public class SecretRedactionServiceTests
{
    private SecretRedactionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new SecretRedactionService();
    }

    [Test]
    public void RedactSecrets_ShouldRedactConnectionString()
    {
        var input = "My connection string is Server=my-db;Database=test;AccountKey=abc123456789+/=;";
        var result = _service.RedactSecrets(input);

        Assert.That(result, Does.Contain("[REDACTED_SECRET]"));
        Assert.That(result, Does.Not.Contain("Server=my-db"));
    }

    [Test]
    public void RedactSecrets_ShouldRedactApiKey()
    {
        var input = "Use api_key=sk-123456789012345678901234 to authenticate.";
        var result = _service.RedactSecrets(input);

        Assert.That(result, Does.Contain("[REDACTED_SECRET]"));
        Assert.That(result, Does.Not.Contain("sk-123456789012345678901234"));
    }

    [Test]
    public void RedactSecrets_ShouldRedactBearerToken()
    {
        var input = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.testToken";
        var result = _service.RedactSecrets(input);

        Assert.That(result, Does.Contain("[REDACTED_SECRET]"));
        Assert.That(result, Does.Not.Contain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"));
    }

    [Test]
    public void RedactSecrets_ShouldLeaveCleanPromptUnchanged()
    {
        var input = "How do I reset my network printer connection?";
        var result = _service.RedactSecrets(input);

        Assert.That(result, Is.EqualTo(input));
    }
}
