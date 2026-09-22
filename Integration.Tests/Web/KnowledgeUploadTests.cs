using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Web.Controllers;

namespace Integration.Tests.Web;

[TestFixture]
public class KnowledgeUploadTests
{
    private FakeKnowledgeBaseService _fakeKbService = null!;
    private FakeKnowledgeSourceStore _fakeSourceStore = null!;
    private FakeKnowledgeIngestionService _fakeIngestionService = null!;
    private FakeAuditLogger _fakeAuditLogger = null!;
    private KnowledgeController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeKbService = new FakeKnowledgeBaseService();
        _fakeSourceStore = new FakeKnowledgeSourceStore();
        _fakeIngestionService = new FakeKnowledgeIngestionService();
        _fakeAuditLogger = new FakeAuditLogger();

        _controller = new KnowledgeController(
            _fakeKbService,
            NullLogger<KnowledgeController>.Instance,
            _fakeSourceStore,
            _fakeIngestionService,
            _fakeAuditLogger
        );

        // Setup Administrator context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "adminUser"),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public void UploadPolicyDocument_HasAuthorizeAdministratorAttribute()
    {
        var method = typeof(KnowledgeController).GetMethod(nameof(KnowledgeController.UploadPolicyDocument));
        Assert.That(method, Is.Not.Null);

        var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(authAttr, Is.Not.Null, "UploadPolicyDocument must have [Authorize] attribute.");
        Assert.That(authAttr!.Roles, Does.Contain("Administrator"), "UploadPolicyDocument must strictly restrict access to Administrator role.");
    }

    [Test]
    public async Task UploadPolicyDocument_ReturnsBadRequest_WhenFileIsNull()
    {
        var result = await _controller.UploadPolicyDocument(null, null, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        Assert.That(badRequest.Value?.ToString(), Does.Contain("Please select a non-empty Markdown"));
    }

    [Test]
    public async Task UploadPolicyDocument_ReturnsBadRequest_WhenFileIsEmpty()
    {
        var file = CreateMockFormFile("empty.md", "");

        var result = await _controller.UploadPolicyDocument(file, null, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        Assert.That(badRequest.Value?.ToString(), Does.Contain("Please select a non-empty Markdown"));
    }

    [TestCase("policy.pdf")]
    [TestCase("script.exe")]
    [TestCase("notes.txt")]
    [TestCase("document.docx")]
    public async Task UploadPolicyDocument_ReturnsBadRequest_WhenFileExtensionIsNotMd(string fileName)
    {
        var file = CreateMockFormFile(fileName, "# Some Policy Content");

        var result = await _controller.UploadPolicyDocument(file, null, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        Assert.That(badRequest.Value?.ToString(), Does.Contain("Only Markdown (.md) policy documents are accepted"));
    }

    [Test]
    public async Task UploadPolicyDocument_ReturnsBadRequest_WhenFileIsBinary()
    {
        var bytes = new byte[] { 0x23, 0x20, 0x54, 0x69, 0x74, 0x6C, 0x65, 0x00, 0x42, 0x69, 0x6E }; // contains null byte
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "policyFile", "binary_policy.md")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/markdown"
        };

        var result = await _controller.UploadPolicyDocument(file, null, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        Assert.That(badRequest.Value?.ToString(), Does.Contain("Binary files are not allowed"));
    }

    [Test]
    public async Task UploadPolicyDocument_SanitizesFileName_AndPreventsPathTraversal()
    {
        var file = CreateMockFormFile("../../evil_policy.md", "# Test Heading\r\nPolicy content here.");

        var result = await _controller.UploadPolicyDocument(file, null, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(_fakeSourceStore.LastUploadedFileName, Does.Not.Contain(".."));
        Assert.That(_fakeSourceStore.LastUploadedFileName, Does.Not.Contain("/"));
        Assert.That(_fakeSourceStore.LastUploadedFileName, Does.Not.Contain("\\"));
        Assert.That(_fakeSourceStore.LastUploadedFileName, Does.StartWith("evil_policy"));
        Assert.That(_fakeSourceStore.LastUploadedFileName, Does.EndWith(".md"));
    }

    [Test]
    public async Task UploadPolicyDocument_SuccessfullyUploads_AndTriggersIngestion_AndInvalidatesCache()
    {
        const string markdown = "---\nDocumentName: New Enterprise Cloud Policy\nSection: Cloud & Infrastructure\n---\n# Cloud Policy\nDetailed guidelines.";
        var file = CreateMockFormFile("cloud_policy.md", markdown);

        var result = await _controller.UploadPolicyDocument(file, "Overridden Title", "Cloud Security", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);

        // Verify storage store called
        Assert.That(_fakeSourceStore.UploadCallCount, Is.EqualTo(1));
        Assert.That(_fakeSourceStore.LastUploadedContent, Does.Contain("DocumentName: Overridden Title"));
        Assert.That(_fakeSourceStore.LastUploadedContent, Does.Contain("Section: Cloud Security"));
        Assert.That(_fakeSourceStore.LastUploadedContent, Does.Contain("Approved: true"));
        Assert.That(_fakeSourceStore.LastUploadedContent, Does.Contain("Active: true"));

        // Verify ingestion triggered
        Assert.That(_fakeIngestionService.IngestCallCount, Is.EqualTo(1));

        // Verify cache invalidated
        Assert.That(_fakeKbService.InvalidateCacheCallCount, Is.EqualTo(1));

        // Verify audit log recorded
        Assert.That(_fakeAuditLogger.LoggedActions.Count, Is.EqualTo(1));
        Assert.That(_fakeAuditLogger.LoggedActions[0].Action, Is.EqualTo("UploadPolicyDocument"));
    }

    [Test]
    public async Task UploadPolicyDocument_Returns500_WhenStorageFails()
    {
        _fakeSourceStore.ShouldThrow = true;
        var file = CreateMockFormFile("cloud_policy.md", "# Valid content");

        var result = await _controller.UploadPolicyDocument(file, null, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objResult = (ObjectResult)result;
        Assert.That(objResult.StatusCode, Is.EqualTo(500));
        Assert.That(objResult.Value?.ToString(), Does.Contain("Failed to store document in Azure Blob Storage"));
    }

    private static IFormFile CreateMockFormFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "policyFile", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/markdown"
        };
    }

    private class FakeKnowledgeBaseService : IKnowledgeBaseService
    {
        public int InvalidateCacheCallCount { get; private set; }

        public Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedDocumentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(Array.Empty<KnowledgeDocumentDto>());

        public Task<KnowledgeDocumentDetailDto?> GetDocumentDetailAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeDocumentDetailDto?>(null);

        public void InvalidateCache()
        {
            InvalidateCacheCallCount++;
        }
    }

    private class FakeKnowledgeSourceStore : IKnowledgeSourceStore
    {
        public int UploadCallCount { get; private set; }
        public string? LastUploadedFileName { get; private set; }
        public string? LastUploadedContent { get; private set; }
        public bool ShouldThrow { get; set; }

        public Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(Array.Empty<KnowledgeDocumentDto>());

        public Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public async Task<KnowledgeDocumentDto> UploadDocumentAsync(string fileName, Stream contentStream, string contentType, CancellationToken cancellationToken = default)
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException("Azure Blob Storage connection failed.");
            }

            UploadCallCount++;
            LastUploadedFileName = fileName;

            using var reader = new StreamReader(contentStream, Encoding.UTF8, leaveOpen: true);
            LastUploadedContent = await reader.ReadToEndAsync(cancellationToken);

            return new KnowledgeDocumentDto(
                Id: Path.GetFileNameWithoutExtension(fileName),
                DocumentName: "Policy Document",
                Version: "1.0",
                Section: "General IT Policy",
                Page: 1,
                Content: LastUploadedContent,
                Approved: true,
                Active: true,
                BlobPath: $"https://azureblob/{fileName}",
                LastModified: DateTime.UtcNow
            );
        }
    }

    private class FakeKnowledgeIngestionService : IKnowledgeIngestionService
    {
        public int IngestCallCount { get; private set; }

        public Task<int> IngestApprovedKnowledgeDocumentsAsync(CancellationToken cancellationToken = default)
        {
            IngestCallCount++;
            return Task.FromResult(5); // 5 chunks ingested
        }
    }

    private class FakeAuditLogger : IAuditLogger
    {
        public List<(string Action, string UserId, string Role, string Details, string IpAddress)> LoggedActions { get; } = new();

        public Task LogActionAsync(string action, string userId, string role, string details, string ipAddress = "", CancellationToken cancellationToken = default)
        {
            LoggedActions.Add((action, userId, role, details, ipAddress));
            return Task.CompletedTask;
        }

        public Task LogActionAsync(string action, string userId, string details, string ipAddress = "", CancellationToken cancellationToken = default)
        {
            LoggedActions.Add((action, userId, string.Empty, details, ipAddress));
            return Task.CompletedTask;
        }
    }
}

