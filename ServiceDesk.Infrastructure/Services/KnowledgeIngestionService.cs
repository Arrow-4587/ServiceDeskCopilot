using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Infrastructure.Services;

public class KnowledgeIngestionService : IKnowledgeIngestionService
{
    private readonly IKnowledgeSourceStore _sourceStore;
    private readonly IKnowledgeIndexStore _indexStore;
    private readonly ILogger<KnowledgeIngestionService> _logger;
    private static readonly SemaphoreSlim IngestionLock = new(1, 1);

    public KnowledgeIngestionService(
        IKnowledgeSourceStore sourceStore,
        IKnowledgeIndexStore indexStore,
        ILogger<KnowledgeIngestionService> logger)
    {
        _sourceStore = sourceStore;
        _indexStore = indexStore;
        _logger = logger;
    }

    public async Task<int> IngestApprovedKnowledgeDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (!await IngestionLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogInformation("[KnowledgeIngestion] Ingestion pipeline is already running. Skipping duplicate concurrent run.");
            return 0;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[KnowledgeIngestion] Starting knowledge ingestion pipeline...");
            var sourceDocs = await _sourceStore.GetApprovedSourceDocumentsAsync(cancellationToken);

            // Grounding Invariant: Only approved AND active documents may be ingested
            var validDocs = sourceDocs.Where(d => d.Approved && d.Active).ToList();
            _logger.LogInformation("[KnowledgeIngestion] Fetched {TotalCount} documents from source store. {ApprovedCount} passed Grounding Invariant filter.", sourceDocs.Count, validDocs.Count);

            var allChunks = new List<KnowledgeDocumentDto>();

            foreach (var doc in validDocs)
            {
                var docChunks = ChunkDocument(doc);
                allChunks.AddRange(docChunks);
            }

            await _indexStore.UpsertChunksBatchAsync(allChunks, cancellationToken);
            sw.Stop();
            _logger.LogInformation("[KnowledgeIngestion] Successfully ingested {ChunkCount} deterministic chunks into knowledge index in {ElapsedMs}ms.", allChunks.Count, sw.ElapsedMilliseconds);

            return allChunks.Count;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[KnowledgeIngestion] Error during knowledge document ingestion pipeline after {ElapsedMs}ms.", sw.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            IngestionLock.Release();
        }
    }

    public static List<KnowledgeDocumentDto> ChunkDocument(KnowledgeDocumentDto doc)
    {
        var chunks = new List<KnowledgeDocumentDto>();
        if (string.IsNullOrWhiteSpace(doc.Content))
            return chunks;

        // Split document into section blocks using markdown headers (# , ## , ### )
        var sections = SplitByMarkdownHeaders(doc.Content, doc.Section);

        foreach (var (sectionTitle, sectionContent) in sections)
        {
            var subChunks = SplitSectionContent(sectionContent, maxCharsPerChunk: 1000);
            int chunkIdx = 0;

            foreach (var chunkText in subChunks)
            {
                chunkIdx++;
                string deterministicId = GenerateDeterministicChunkId(doc.DocumentName, sectionTitle, doc.Page, chunkIdx);

                var chunkDto = new KnowledgeDocumentDto(
                    Id: deterministicId,
                    DocumentName: doc.DocumentName,
                    Version: doc.Version,
                    Section: sectionTitle,
                    Page: doc.Page,
                    Content: chunkText,
                    Approved: doc.Approved,
                    Active: doc.Active,
                    BlobPath: doc.BlobPath,
                    LastModified: doc.LastModified
                );

                chunks.Add(chunkDto);
            }
        }

        return chunks;
    }

    private static List<(string title, string content)> SplitByMarkdownHeaders(string rawContent, string defaultSection)
    {
        var results = new List<(string title, string content)>();
        var lines = rawContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        string currentTitle = defaultSection;
        var currentBuffer = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("# ") || line.StartsWith("## ") || line.StartsWith("### "))
            {
                if (currentBuffer.Length > 0)
                {
                    var text = currentBuffer.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        results.Add((currentTitle, text));
                    }
                    currentBuffer.Clear();
                }

                currentTitle = line.TrimStart('#', ' ').Trim();
            }
            else
            {
                currentBuffer.AppendLine(line);
            }
        }

        if (currentBuffer.Length > 0)
        {
            var text = currentBuffer.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add((currentTitle, text));
            }
        }

        if (results.Count == 0 && !string.IsNullOrWhiteSpace(rawContent))
        {
            results.Add((defaultSection, rawContent.Trim()));
        }

        return results;
    }

    private static List<string> SplitSectionContent(string content, int maxCharsPerChunk)
    {
        var chunks = new List<string>();
        if (content.Length <= maxCharsPerChunk)
        {
            chunks.Add(content);
            return chunks;
        }

        var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var p in paragraphs)
        {
            if (currentChunk.Length + p.Length + 2 > maxCharsPerChunk && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }

            if (p.Length > maxCharsPerChunk)
            {
                // Fallback line-by-line split for oversized single paragraphs
                var lines = p.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (currentChunk.Length + line.Length + 1 > maxCharsPerChunk && currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.ToString().Trim());
                        currentChunk.Clear();
                    }
                    currentChunk.AppendLine(line);
                }
            }
            else
            {
                currentChunk.AppendLine(p);
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    public static string GenerateDeterministicChunkId(string docName, string section, int page, int chunkIndex)
    {
        string rawKey = $"{docName.ToLowerInvariant()}|{section.ToLowerInvariant()}|{page}|{chunkIndex}";
        byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
