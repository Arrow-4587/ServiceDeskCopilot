using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Web.Models;

public class HomeViewModel
{
    public string UserRole { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public List<User> RegisteredUsers { get; set; } = new();
    public List<KnowledgeFileItem> KnowledgeFiles { get; set; } = new();
    public IReadOnlyList<KnowledgeDocumentDto> KnowledgeDocuments { get; set; } = Array.Empty<KnowledgeDocumentDto>();
    public List<ConversationSession> UserChatHistory { get; set; } = new();
    public List<IncidentDraft> UserTickets { get; set; } = new();
    public List<IncidentDraft> AllTickets { get; set; } = new();
    public List<ConversationSession> AllChatHistory { get; set; } = new();
    public Dictionary<string, int> EmployeeTicketStats { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
}

public class KnowledgeFileItem
{
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
