namespace ServiceDesk.Domain.ValueObjects;

public record Citation
{
    public string DocumentName { get; init; }
    public string Section { get; init; }
    public int Page { get; init; }
    public string Snippet { get; init; }
    public string BlobPath { get; init; }

    public Citation(string documentName, string section, int page, string snippet, string blobPath = "")
    {
        DocumentName = documentName ?? string.Empty;
        Section = section ?? string.Empty;
        Page = page;
        Snippet = snippet ?? string.Empty;
        BlobPath = blobPath ?? string.Empty;
    }
}
