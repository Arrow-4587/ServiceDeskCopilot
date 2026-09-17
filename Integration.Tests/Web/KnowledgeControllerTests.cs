using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Web.Controllers;

namespace Integration.Tests.Web;

[TestFixture]
public class KnowledgeControllerTests
{
    private FakeKnowledgeBaseService _fakeKbService = null!;
    private KnowledgeController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeKbService = new FakeKnowledgeBaseService();
        _controller = new KnowledgeController(_fakeKbService, NullLogger<KnowledgeController>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public async Task KnowledgeController_Index_ReturnsViewResultWithDocuments()
    {
        _fakeKbService.Documents = new List<KnowledgeDocumentDto>
        {
            new("vpn_policy", "VPN Policy", "1.0", "Network", 1, "VPN Content", true, true, "blob/vpn.md", DateTime.UtcNow),
            new("mfa_guide", "MFA Setup Guide", "1.1", "Security", 1, "MFA Content", true, true, "blob/mfa.md", DateTime.UtcNow)
        };

        var result = await _controller.Index(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(viewResult.Model, Is.InstanceOf<IReadOnlyList<KnowledgeDocumentDto>>());
        var docs = (IReadOnlyList<KnowledgeDocumentDto>)viewResult.Model!;
        Assert.That(docs.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task KnowledgeController_GetDocuments_ReturnsJsonResultWithApprovedDocs()
    {
        _fakeKbService.Documents = new List<KnowledgeDocumentDto>
        {
            new("vpn_policy", "VPN Policy", "1.0", "Network", 1, "VPN Content", true, true, "blob/vpn.md", DateTime.UtcNow)
        };

        var result = await _controller.GetDocuments(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task KnowledgeController_GetDocument_ReturnsDetailJsonResult()
    {
        _fakeKbService.Documents = new List<KnowledgeDocumentDto>
        {
            new("vpn_policy", "VPN Policy", "1.0", "Network", 1, "# VPN Policy Header\n\nContent here", true, true, "blob/vpn.md", DateTime.UtcNow)
        };

        var result = await _controller.GetDocument("vpn_policy", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task KnowledgeController_GetDocument_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        var result = await _controller.GetDocument("missing_id", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task KnowledgeController_GetDocuments_Returns500_WhenAzureSourceThrows()
    {
        _fakeKbService.ShouldThrowException = true;

        var result = await _controller.GetDocuments(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objResult = (ObjectResult)result;
        Assert.That(objResult.StatusCode, Is.EqualTo(500), "Azure storage failures must return HTTP 500 status rather than silent fallback.");
    }

    private class FakeKnowledgeBaseService : IKnowledgeBaseService
    {
        public List<KnowledgeDocumentDto> Documents { get; set; } = new();
        public bool ShouldThrowException { get; set; }

        public Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedDocumentsAsync(CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
            {
                throw new InvalidOperationException("Azure Blob Storage container is unavailable.");
            }
            return Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(Documents.Where(d => d.Approved && d.Active).ToList());
        }

        public Task<KnowledgeDocumentDetailDto?> GetDocumentDetailAsync(string id, CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
            {
                throw new InvalidOperationException("Azure Blob Storage container is unavailable.");
            }
            var doc = Documents.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && d.Approved && d.Active);
            if (doc == null) return Task.FromResult<KnowledgeDocumentDetailDto?>(null);

            var detail = new KnowledgeDocumentDetailDto(doc, $"<h1>{doc.DocumentName}</h1><p>{doc.Content}</p>");
            return Task.FromResult<KnowledgeDocumentDetailDto?>(detail);
        }
    }
}
