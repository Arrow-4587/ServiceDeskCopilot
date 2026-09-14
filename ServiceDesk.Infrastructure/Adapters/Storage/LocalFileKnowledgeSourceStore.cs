using Microsoft.Extensions.Configuration;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Infrastructure.Adapters.Storage;

public class LocalFileKnowledgeSourceStore : IKnowledgeSourceStore
{
    private readonly string _knowledgeFolder;

    public LocalFileKnowledgeSourceStore(IConfiguration configuration)
    {
        var baseDir = AppContext.BaseDirectory;
        var relativePath = Path.Combine(baseDir, "..", "..", "..", "..", "data", "knowledge");
        
        if (Directory.Exists(relativePath))
        {
            _knowledgeFolder = Path.GetFullPath(relativePath);
        }
        else
        {
            _knowledgeFolder = Path.Combine(baseDir, "data", "knowledge");
        }
    }

    public Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var documents = new List<KnowledgeDocumentDto>();

        if (!Directory.Exists(_knowledgeFolder))
            return Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(documents);

        var files = Directory.GetFiles(_knowledgeFolder, "*.md");

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var (docName, version, section, page, approved, active, body) = ParseFrontmatter(file, content);

            // Carry BlobPath / FilePath through to metadata for citation traceability (Spec Section 6A)
            var relativeBlobPath = Path.Combine("data", "knowledge", Path.GetFileName(file)).Replace('\\', '/');

            var doc = new KnowledgeDocumentDto(
                Id: Path.GetFileNameWithoutExtension(file),
                DocumentName: docName,
                Version: version,
                Section: section,
                Page: page,
                Content: body,
                Approved: approved,
                Active: active,
                BlobPath: relativeBlobPath,
                LastModified: File.GetLastWriteTimeUtc(file)
            );

            documents.Add(doc);
        }

        return Task.FromResult<IReadOnlyList<KnowledgeDocumentDto>>(documents);
    }

    public Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_knowledgeFolder, Path.GetFileName(blobPath));
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    private static (string docName, string version, string section, int page, bool approved, bool active, string body) ParseFrontmatter(string filePath, string fileContent)
    {
        var docName = Path.GetFileNameWithoutExtension(filePath);
        var version = "1.0";
        var section = "General";
        var page = 1;
        var approved = true;
        var active = true;
        var body = fileContent;

        if (fileContent.StartsWith("---"))
        {
            var parts = fileContent.Split(new[] { "---" }, 3, StringSplitOptions.None);
            if (parts.Length >= 3)
            {
                var frontmatterLines = parts[1].Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                body = parts[2].Trim();

                foreach (var line in frontmatterLines)
                {
                    var kv = line.Split(new[] { ':' }, 2);
                    if (kv.Length < 2) continue;

                    var key = kv[0].Trim();
                    var val = kv[1].Trim();

                    switch (key.ToLowerInvariant())
                    {
                        case "documentname":
                            docName = val;
                            break;
                        case "version":
                            version = val;
                            break;
                        case "section":
                            section = val;
                            break;
                        case "page":
                            if (int.TryParse(val, out var p)) page = p;
                            break;
                        case "approved":
                            if (bool.TryParse(val, out var app)) approved = app;
                            break;
                        case "active":
                            if (bool.TryParse(val, out var act)) active = act;
                            break;
                    }
                }
            }
        }

        return (docName, version, section, page, approved, active, body);
    }
}
