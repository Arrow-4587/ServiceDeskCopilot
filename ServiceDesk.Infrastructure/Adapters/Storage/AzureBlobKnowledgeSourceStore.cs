using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Infrastructure.Adapters.Storage;

public class AzureBlobKnowledgeSourceStore : IKnowledgeSourceStore
{
    private readonly ILogger<AzureBlobKnowledgeSourceStore> _logger;
    private readonly string _connectionString;
    private readonly string _containerName;

    public AzureBlobKnowledgeSourceStore(
        IConfiguration configuration,
        ILogger<AzureBlobKnowledgeSourceStore> logger)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? string.Empty;
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "knowledge";
    }

    public async Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Azure Blob Storage connection is required. Local knowledge files are disabled.");
        }

        try
        {
            _logger.LogInformation("[AzureBlobStorage] Connecting to Azure Blob container '{ContainerName}' to fetch knowledge documents...", _containerName);
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            var exists = await containerClient.ExistsAsync(cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException($"Azure Blob Storage container '{_containerName}' does not exist.");
            }

            var documents = new List<KnowledgeDocumentDto>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                if (!blobItem.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    continue;

                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var downloadResult = await blobClient.DownloadContentAsync(cancellationToken);
                var content = downloadResult.Value.Content.ToString();

                var (docName, version, section, page, approved, active, body) = ParseFrontmatter(blobItem.Name, content);

                var doc = new KnowledgeDocumentDto(
                    Id: Path.GetFileNameWithoutExtension(blobItem.Name),
                    DocumentName: docName,
                    Version: version,
                    Section: section,
                    Page: page,
                    Content: body,
                    Approved: approved,
                    Active: active,
                    BlobPath: blobClient.Uri.ToString(),
                    LastModified: blobItem.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow
                );

                documents.Add(doc);
            }

            _logger.LogInformation("[AzureBlobStorage] Successfully fetched {Count} knowledge documents from Azure Blob container '{ContainerName}'.", documents.Count, _containerName);
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorage] Error fetching documents from Azure Blob Storage.");
            throw;
        }
    }

    public async Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Azure Blob Storage connection is required. Local knowledge files are disabled.");
        }

        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = Path.GetFileName(blobPath);
            var blobClient = containerClient.GetBlobClient(blobName);

            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists)
            {
                throw new FileNotFoundException($"Azure Blob '{blobPath}' does not exist.");
            }

            var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorage] Failed to stream blob '{BlobPath}'.", blobPath);
            throw;
        }
    }

    private static (string docName, string version, string section, int page, bool approved, bool active, string body) ParseFrontmatter(string fileName, string content)
    {
        var docName = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ');
        var version = "1.0";
        var section = "General";
        var page = 1;
        var approved = true;
        var active = true;
        var body = content;

        if (content.StartsWith("---"))
        {
            var parts = content.Split(new[] { "---" }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var yaml = parts[0];
                body = parts[1].Trim();

                foreach (var line in yaml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = line.Split(':', 2);
                    if (kv.Length == 2)
                    {
                        var key = kv[0].Trim().ToLowerInvariant();
                        var val = kv[1].Trim().Trim('"', '\'');

                        switch (key)
                        {
                            case "title": docName = val; break;
                            case "version": version = val; break;
                            case "section": section = val; break;
                            case "page": int.TryParse(val, out page); break;
                            case "approved": bool.TryParse(val, out approved); break;
                            case "active": bool.TryParse(val, out active); break;
                        }
                    }
                }
            }
        }

        return (docName, version, section, page, approved, active, body);
    }
}
