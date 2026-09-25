using System.Diagnostics;
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
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly BlobContainerClient? _containerClient;

    public AzureBlobKnowledgeSourceStore(
        IConfiguration configuration,
        ILogger<AzureBlobKnowledgeSourceStore> logger)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? string.Empty;
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "knowledge";

        if (!string.IsNullOrWhiteSpace(_connectionString) && !_connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            _blobServiceClient = new BlobServiceClient(_connectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        }
    }

    public async Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || _containerClient == null || _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Azure Blob Storage connection is required. Local knowledge files are disabled.");
        }

        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[AzureBlobStorage] Connecting to Azure Blob container '{ContainerName}' to fetch knowledge documents...", _containerName);

            var exists = await _containerClient.ExistsAsync(cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException($"Azure Blob Storage container '{_containerName}' does not exist.");
            }

            var blobItems = new List<BlobItem>();

            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                if (!blobItem.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    continue;

                blobItems.Add(blobItem);
            }

            var downloadTasks = blobItems.Select(async blobItem =>
            {
                var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                var downloadResult = await blobClient.DownloadContentAsync(cancellationToken);
                var content = downloadResult.Value.Content.ToString();

                var (docName, version, section, page, approved, active, body) = ParseFrontmatter(blobItem.Name, content);

                return new KnowledgeDocumentDto(
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
            });

            var documents = (await Task.WhenAll(downloadTasks)).ToList();

            sw.Stop();
            _logger.LogInformation("[AzureBlobStorage] Successfully fetched {Count} knowledge documents in parallel from container '{ContainerName}' in {ElapsedMs}ms.", documents.Count, _containerName, sw.ElapsedMilliseconds);
            return documents;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[AzureBlobStorage] Error fetching documents from Azure Blob Storage after {ElapsedMs}ms.", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || _containerClient == null || _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Azure Blob Storage connection is required. Local knowledge files are disabled.");
        }

        try
        {
            var blobName = Path.GetFileName(blobPath);
            var blobClient = _containerClient.GetBlobClient(blobName);

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
            var parts = content.Split(new[] { "---" }, 3, StringSplitOptions.None);
            if (parts.Length >= 3)
            {
                var yaml = parts[1];
                body = parts[2].Trim();

                foreach (var line in yaml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = line.Split(':', 2);
                    if (kv.Length == 2)
                    {
                        var key = kv[0].Trim().ToLowerInvariant();
                        var val = kv[1].Trim().Trim('"', '\'');

                        switch (key)
                        {
                            case "title":
                            case "documentname":
                                if (!string.IsNullOrWhiteSpace(val)) docName = val;
                                break;
                            case "version":
                                if (!string.IsNullOrWhiteSpace(val)) version = val;
                                break;
                            case "section":
                                if (!string.IsNullOrWhiteSpace(val)) section = val;
                                break;
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

    public async Task<KnowledgeDocumentDto> UploadDocumentAsync(string fileName, Stream contentStream, string contentType = "text/markdown", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || _containerClient == null || _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Azure Blob Storage connection is required. Uploading documents to local file storage is disabled in this environment.");
        }

        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (contentStream == null) throw new ArgumentNullException(nameof(contentStream));

        try
        {
            await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = _containerClient.GetBlobClient(fileName);
            contentStream.Position = 0;

            using var reader = new StreamReader(contentStream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var content = await reader.ReadToEndAsync(cancellationToken);
            contentStream.Position = 0;

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "text/markdown"
                }
            };

            await blobClient.UploadAsync(contentStream, uploadOptions, cancellationToken);
            _logger.LogInformation("[AzureBlobStorage] Successfully uploaded policy document '{BlobName}' to Azure container '{ContainerName}'.", fileName, _containerName);

            var (parsedName, version, parsedSection, page, approved, active, body) = ParseFrontmatter(fileName, content);
            var effectiveTitle = parsedName;
            var effectiveSection = parsedSection;

            return new KnowledgeDocumentDto(
                Id: Path.GetFileNameWithoutExtension(fileName),
                DocumentName: effectiveTitle,
                Version: version,
                Section: effectiveSection,
                Page: page,
                Content: body,
                Approved: approved,
                Active: active,
                BlobPath: blobClient.Uri.ToString(),
                LastModified: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorage] Failed to upload document '{FileName}' to Azure Blob Storage.", fileName);
            throw;
        }
    }
}
